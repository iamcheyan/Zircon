#!/usr/bin/env python3
"""measure_frame50_details.py — Find exact center and bounding boxes for Left Ball and Right Compass."""
from PIL import Image

def main():
    im = Image.open("/tmp/frame50_debug.png").convert("RGBA")

    # 1. Left Circle Orifice (HP/MP Orifice)
    # The circle is located in X: 50..180, Y: 10..130
    min_x, max_x, min_y, max_y = 800, 0, 136, 0
    for x in range(50, 185):
        for y in range(10, 130):
            r, g, b, a = im.getpixel((x, y))
            # The inner circle is transparent (a < 50) where HP/MP ball shows through!
            if a < 50:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y

    print(f"Left HP/MP Transparent Circle Hole: X={min_x}..{max_x} (W={max_x-min_x+1}), Y={min_y}..{max_y} (H={max_y-min_y+1})")

    # 2. Right Compass Center (Right Compass Orifice)
    # The right compass center is in X: 600..750, Y: 20..130
    c_min_x, c_max_x, c_min_y, c_max_y = 800, 0, 136, 0
    for x in range(610, 740):
        for y in range(20, 120):
            r, g, b, a = im.getpixel((x, y))
            # Center hole in compass
            if a < 50:
                if x < c_min_x: c_min_x = x
                if x > c_max_x: c_max_x = x
                if y < c_min_y: c_min_y = y
                if y > c_max_y: c_max_y = y

    print(f"Right Compass Center Hole: X={c_min_x}..{c_max_x} (W={c_max_x-c_min_x+1}), Y={c_min_y}..{c_max_y} (H={c_max_y-c_min_y+1})")

    # 3. Chat Log Window Box
    # X: 200..570, Y: 20..120
    # Chat box inner dark area
    chat_min_x, chat_max_x, chat_min_y, chat_max_y = 800, 0, 136, 0
    for x in range(200, 580):
        for y in range(20, 120):
            r, g, b, a = im.getpixel((x, y))
            if r < 30 and g < 30 and b < 30 and a > 200:
                if x < chat_min_x: chat_min_x = x
                if x > chat_max_x: chat_max_x = x
                if y < chat_min_y: chat_min_y = y
                if y > chat_max_y: chat_max_y = y
    print(f"Chat Log Window Box: X={chat_min_x}..{chat_max_x} (W={chat_max_x-chat_min_x+1}), Y={chat_min_y}..{chat_max_y} (H={chat_max_y-chat_min_y+1})")

if __name__ == "__main__":
    main()
