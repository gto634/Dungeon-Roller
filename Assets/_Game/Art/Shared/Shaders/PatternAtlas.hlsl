#ifndef PATTERN_ATLAS_INCLUDED
#define PATTERN_ATLAS_INCLUDED


#define PATTERN_COLOR_COUNT 9.0
#define PATTERN_BAND_COUNT 7.0
#define LAST_COLUMN (PATTERN_COLOR_COUNT - 1.0)
#define LAST_BAND (PATTERN_BAND_COUNT - 1.0)
#define AUTO_COLOR PATTERN_COLOR_COUNT
#define CUSTOM_COLOR (PATTERN_COLOR_COUNT + 1.0)
#define SAME_BAND PATTERN_BAND_COUNT


float PatternOutlineCode(float color, float darkness)
{
    return clamp(round(color), 0.0, LAST_COLUMN) + 1.0 + saturate(darkness) * 0.5;
}

float PatternOutlineDarkness(float alpha, float code)
{
    return saturate((alpha - code) * 2.0);
}

float2 PatternTileSize()
{
    return _PatternAtlas_TexelSize.zw / float2(PATTERN_COLOR_COUNT, PATTERN_BAND_COUNT + 1.0);
}

float2 PatternPalettePixel(float index)
{
    float2 tile = PatternTileSize();
    return float2((clamp(round(index), 0.0, LAST_COLUMN) + 0.5) * tile.x,
                  _PatternAtlas_TexelSize.w - tile.y * 0.5);
}

#endif
