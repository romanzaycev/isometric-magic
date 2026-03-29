#!/bin/bash

# Check if ImageMagick (convert) is installed
if ! command -v convert &> /dev/null; then
    echo "Error: ImageMagick (convert) is not installed. Please install it before running."
    exit 1
fi

INVERT=0

# Parse command-line arguments
for arg in "$@"; do
    case $arg in
        --invert)
        INVERT=1
        shift
        ;;
        -h|--help)
        echo "Usage: $0 [--invert]"
        echo "  Default  : Black becomes transparent, the rest becomes white."
        echo "  --invert : White becomes transparent, the rest becomes white."
        exit 0
        ;;
    esac
done

# Create an output directory
OUTPUT_DIR="processed_masks"
mkdir -p "$OUTPUT_DIR"

# Flag to verify if any files were processed
files_found=0

# Loop through all PNG files in the current directory
for img in *.png; do
    # If no files are found, the loop captures the literal string "*.png", so we skip it
    [ -e "$img" ] || continue
    files_found=1
    
    filename=$(basename "$img")
    
    if [ "$INVERT" -eq 1 ]; then
        # -negate: inverts colors (white becomes black and vice versa)
        # -alpha copy: uses grayscale values as the alpha channel (black=transparent, white=opaque)
        # -fill white -colorize 100%: makes all visible pixels pure white
        convert "$img" -negate -alpha copy -fill white -colorize 100% "$OUTPUT_DIR/$filename"
        echo "Processed (inverted): $filename"
    else
        convert "$img" -alpha copy -fill white -colorize 100% "$OUTPUT_DIR/$filename"
        echo "Processed: $filename"
    fi
done

if [ "$files_found" -eq 0 ]; then
    echo "No .png files found in the current directory!"
else
    echo "Done! All masks have been saved to the '$OUTPUT_DIR' directory."
fi
