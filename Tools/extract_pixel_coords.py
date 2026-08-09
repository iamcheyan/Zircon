#!/usr/bin/env python3
"""extract_pixel_coords.py — Measure exact pixel bounds on Frame 50 image."""
from PIL import Image

def main():
    im = Image.open("/tmp/frame50_debug.png").convert("RGBA")
    print("Frame 50 size:", im.size) # (800, 136)

    # 1. Measure Left HP/MP Circle orifice bounds:
    # Scan bounding box of non-transparent/empty circle on left
    # Left circle is roughly X: 55..165, Y: 10..120
    
    # 2. Measure top experience bar groove:
    # Yellow gem is around X: 360..380
    # Groove is between X: 260..540, Y: 0..10

    # Print pixel rows around experience groove
    print("Scanning top groove Y=0..15 around center X=350..450...")
    for y in range(0, 15):
        row_str = "".join("#" if im.getpixel((x, y))[3] > 128 else "." for x in range(350, 420))
        print(f"Y={y:2d}: {row_str[:50]}")

if __name__ == "__main__":
    main()
