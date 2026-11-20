Nebula Drift
============

Stylized Sci-Fi Space Skyboxes — Vol. 1

3 seamless, stylised procedural space skyboxes. 3 of 3 skies include a genuine scene-referred HDR (.hdr) master (the sun/core is encoded far above display white — real key light for IBL).

CONTENTS (per sky)
------------------
  <Sky>_Equirect_<TIER>.png    equirectangular 2:1 master (2K)
  cubemap_<TIER>/{px,nx,py,ny,pz,nz}.png   6 GL cube faces per tier
  <Sky>_cubemap_cross.png      cross-layout overview of the 6 faces
  <Sky>_Equirect_2K.hdr        scene-referred HDR (HDR skies only)

SKIES
-----
  - Emerald Veil       HDR (peak 80x)
  - Crimson Expanse    HDR (peak 54x)
  - Abyssal Void       HDR (peak 63x)

USAGE
-----
  Equirect: map onto a sky-sphere/dome or use as an HDRI-style environment.
  Cubemap : px/nx/py/ny/pz/nz follow the OpenGL convention (forward +Z, up +Y);
            remap axes per engine (Unity/Unreal import notes in listing.md).
  HDR     : Radiance .hdr (RGBE) — load as an environment/IBL light source.
  Seamless across the longitude wrap; poles meet at zenith/nadir as usual for
  equirectangular maps.

NOTES
-----
  - The generator is resolution-independent (tested at 8K); tiers are capped at 4K
    here because stylized nebulae hold no detail beyond it. 8K / .exr on request.
  - HDR shipped at 2K: ample for IBL of this low-frequency content; the higher-res
    detail lives in the 4K PNG/cubemap backgrounds.

100% procedural — no AI, no scraped data. See LICENSE.txt.
Generated with asset-forge.
