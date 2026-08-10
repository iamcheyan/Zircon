#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wil_probe.py — WIL 素材存在性探测。

语义（A1 真打基础）: 某条目在某客户端存在素材
  ⇔ 该客户端 Data/ 目录存在对应图库 且 指定帧能解出非空图像。

ei / mei 版本标签 = 素材存在性（WIL 可解出帧），不代表服务端内容存在性；
服务端内容存在性由 ver 的 mud3/zircon 标签承担（three_versions / DAT 对照）。
"""
import os

import wilsdk

EI_DATA = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"
MEI_DATA = "/home/tetsuya/NAS/TMP/mir3ei/Data"

_cache = {}  # (data_dir, lib_lower) -> WilLibrary | None


def get_lib(data_dir, lib):
    key = (data_dir, lib.lower())
    if key in _cache:
        return _cache[key]
    wl = None
    if os.path.isdir(data_dir):
        for fname in os.listdir(data_dir):
            if fname.lower() == lib.lower():
                try:
                    wl = wilsdk.open_library(os.path.join(data_dir, fname))
                except Exception:
                    wl = None
                break
    _cache[key] = wl
    return wl


def frame_ok(data_dir, lib, frame):
    """图库存在且指定帧解出非空图像 → True。"""
    if not lib or not str(lib).lower().endswith(".wil"):
        return False  # Zl/ZL2 是 Zircon 私有格式, 不探测客户端素材
    wl = get_lib(data_dir, lib)
    if wl is None:
        return False
    try:
        if frame >= wl.count:
            return False
        im = wl.decode(frame)
    except Exception:
        return False
    return im is not None and im.getbbox() is not None


def client_tags(lib, frame):
    """EI / mir3ei 素材存在性 → ('ei',) / ('mei',) / ('ei','mei') / ()。"""
    out = []
    if frame_ok(EI_DATA, lib, frame):
        out.append("ei")
    if frame_ok(MEI_DATA, lib, frame):
        out.append("mei")
    return out
