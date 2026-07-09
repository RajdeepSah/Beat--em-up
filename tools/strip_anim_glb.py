#!/usr/bin/env python3
"""
Strip a Higgsfield one-clip animation GLB down to hierarchy + animation curves.

The 3d_rigging output carries the full mesh + texture (~7 MB) that the game already
has in the base model. For clip playback, glTFast only needs the node hierarchy and
the animation — so this drops meshes/skins/materials/textures/images and repacks the
binary buffer with just the animation sampler data (~0.2-0.5 MB per clip).

Also validates the pipeline invariants from Docs/AAA_UPGRADE_PLAN.md §4.3:
  * node paths must EXACTLY match the base character GLB (legacy clips bind by
    node-path string and fail silently on mismatch) — hard error otherwise;
  * exactly one animation per file;
  * --inplace: remove root travel (delta-zero horizontal translation of the topmost
    translation-animated node) for locomotion clips, since movement is code-driven.

Usage:
  python3 tools/strip_anim_glb.py IN.glb OUT.glb --base BASE.glb [--inplace]
"""
import argparse
import json
import struct
import sys

GLB_MAGIC = 0x46546C67
CHUNK_JSON = 0x4E4F534A
CHUNK_BIN = 0x004E4942

COMPONENT_SIZE = {5120: 1, 5121: 1, 5122: 2, 5123: 2, 5125: 4, 5126: 4}
TYPE_COUNT = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def read_glb(path):
    with open(path, "rb") as f:
        magic, _version, _length = struct.unpack("<III", f.read(12))
        if magic != GLB_MAGIC:
            sys.exit(f"{path}: not a GLB file")
        gltf, binbuf = None, b""
        while True:
            header = f.read(8)
            if len(header) < 8:
                break
            clen, ctype = struct.unpack("<II", header)
            data = f.read(clen)
            if ctype == CHUNK_JSON:
                gltf = json.loads(data)
            elif ctype == CHUNK_BIN:
                binbuf = data
        return gltf, binbuf


def write_glb(path, gltf, binbuf):
    payload = json.dumps(gltf, separators=(",", ":")).encode()
    payload += b" " * (-len(payload) % 4)
    binbuf += b"\x00" * (-len(binbuf) % 4)
    total = 12 + 8 + len(payload) + 8 + len(binbuf)
    with open(path, "wb") as f:
        f.write(struct.pack("<III", GLB_MAGIC, 2, total))
        f.write(struct.pack("<II", len(payload), CHUNK_JSON))
        f.write(payload)
        f.write(struct.pack("<II", len(binbuf), CHUNK_BIN))
        f.write(binbuf)


def node_paths(gltf):
    nodes = gltf.get("nodes", [])
    paths = {}

    def walk(i, prefix):
        name = nodes[i].get("name", f"node_{i}")
        p = f"{prefix}/{name}" if prefix else name
        paths[i] = p
        for c in nodes[i].get("children", []):
            walk(c, p)

    for scene in gltf.get("scenes", []):
        for root in scene.get("nodes", []):
            walk(root, "")
    return paths


def node_depths(gltf):
    nodes = gltf.get("nodes", [])
    depth = {}

    def walk(i, d):
        depth[i] = d
        for c in nodes[i].get("children", []):
            walk(c, d + 1)

    for scene in gltf.get("scenes", []):
        for root in scene.get("nodes", []):
            walk(root, 0)
    return depth


def accessor_bytes(gltf, binbuf, idx):
    acc = gltf["accessors"][idx]
    bv = gltf["bufferViews"][acc["bufferView"]]
    start = bv.get("byteOffset", 0) + acc.get("byteOffset", 0)
    length = COMPONENT_SIZE[acc["componentType"]] * TYPE_COUNT[acc["type"]] * acc["count"]
    return start, length, acc


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--base", required=True, help="base character GLB the clip must bind to")
    ap.add_argument("--inplace", action="store_true", help="remove horizontal root travel")
    args = ap.parse_args()

    gltf, binbuf = read_glb(args.input)
    base_gltf, _ = read_glb(args.base)

    # --- Invariant 1: exactly one animation ---
    anims = gltf.get("animations", [])
    if len(anims) != 1:
        sys.exit(f"FAIL {args.input}: expected exactly 1 animation, found {len(anims)}")
    anim = anims[0]

    # --- Invariant 2: node paths match the base character exactly ---
    mine, base = set(node_paths(gltf).values()), set(node_paths(base_gltf).values())
    if mine != base:
        only_mine, only_base = sorted(mine - base)[:5], sorted(base - mine)[:5]
        sys.exit(f"FAIL {args.input}: node paths differ from base\n  extra: {only_mine}\n  missing: {only_base}")

    binbuf = bytearray(binbuf)

    # --- Optional: delta-zero horizontal translation of the topmost animated node ---
    if args.inplace:
        depths = node_depths(gltf)
        trans_channels = [c for c in anim["channels"] if c["target"].get("path") == "translation"]
        if trans_channels:
            top = min(trans_channels, key=lambda c: depths.get(c["target"]["node"], 99))
            sampler = anim["samplers"][top["sampler"]]
            start, length, acc = accessor_bytes(gltf, binbuf, sampler["output"])
            if acc["componentType"] == 5126 and acc["type"] == "VEC3":
                x0, _, z0 = struct.unpack_from("<3f", binbuf, start)
                for k in range(acc["count"]):
                    off = start + k * 12
                    _, y, _ = struct.unpack_from("<3f", binbuf, off)
                    struct.pack_into("<3f", binbuf, off, x0, y, z0)
                acc.pop("min", None)
                acc.pop("max", None)
                print(f"  in-place: pinned horizontal travel on node "
                      f"{gltf['nodes'][top['target']['node']].get('name', '?')} ({acc['count']} keys)")

    # --- Strip everything except hierarchy + animation ---
    keep_accessors = sorted({s["input"] for s in anim["samplers"]} | {s["output"] for s in anim["samplers"]})
    acc_remap = {old: new for new, old in enumerate(keep_accessors)}

    new_accessors, new_views, new_bin = [], [], bytearray()
    for old in keep_accessors:
        start, length, acc = accessor_bytes(gltf, binbuf, old)
        new_views.append({"buffer": 0, "byteOffset": len(new_bin), "byteLength": length})
        new_bin += binbuf[start:start + length]
        new_bin += b"\x00" * (-len(new_bin) % 4)
        a = dict(acc)
        a["bufferView"] = len(new_views) - 1
        a.pop("byteOffset", None)
        new_accessors.append(a)

    for s in anim["samplers"]:
        s["input"] = acc_remap[s["input"]]
        s["output"] = acc_remap[s["output"]]

    for n in gltf.get("nodes", []):
        n.pop("mesh", None)
        n.pop("skin", None)

    for key in ("meshes", "skins", "materials", "textures", "images", "samplers", "cameras"):
        gltf.pop(key, None)

    gltf["accessors"] = new_accessors
    gltf["bufferViews"] = new_views
    gltf["buffers"] = [{"byteLength": len(new_bin)}]

    write_glb(args.output, gltf, bytes(new_bin))
    import os
    print(f"OK {args.output}: {os.path.getsize(args.input) / 1e6:.1f} MB -> {os.path.getsize(args.output) / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
