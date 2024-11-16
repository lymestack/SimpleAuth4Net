# Example Usage: python resize-images.py /path/to/your/image/directory 0.5

import os
from PIL import Image
import argparse

def resize_images(directory, scale_factor):
    """
    Resize all images in the given directory by the specified scale factor.

    :param directory: Path to the directory containing images.
    :param scale_factor: Factor by which to scale the images (e.g., 0.5 for 50%).
    """
    for filename in os.listdir(directory):
        if filename.lower().endswith(('.png', '.jpg', '.jpeg')):
            file_path = os.path.join(directory, filename)
            try:
                with Image.open(file_path) as img:
                    # Calculate the new dimensions
                    new_width = int(img.width * scale_factor)
                    new_height = int(img.height * scale_factor)
                    # Resize the image
                    resized_img = img.resize((new_width, new_height), Image.Resampling.LANCZOS)
                    # Save the resized image
                    resized_img.save(file_path)
                    print(f"Resized {filename} to {new_width}x{new_height}")
            except Exception as e:
                print(f"Failed to resize {filename}: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Resize all images in a directory by a scale factor.")
    parser.add_argument("directory", type=str, help="The path to the directory containing images.")
    parser.add_argument("scale_factor", type=float, help="The factor by which to scale the images (e.g., 0.5 for 50%).")

    args = parser.parse_args()
    resize_images(args.directory, args.scale_factor)
