# Willow logo — Nano Banana prompt

Starter prompt for generating the Willow logo with **Nano Banana**
(Google's Gemini 2.5 Flash Image). The product framing is: *a tool that
prunes a .NET project's dependency tree*. Same metaphor as the original
bonsai mark, but the tree is now a willow — characteristically tall and
slender, with long drooping branches that catch the eye.

Iterate freely once the first generation comes back — Nano Banana keeps
style across follow-up edits ("now thicker trunk", "swap pot to slate",
"remove the bird", etc.).

---

## Primary prompt

> A clean, modern minimalist logo of a **weeping willow tree**, designed
> as a tech product mark for a .NET developer tool. The willow has a
> single slender trunk that branches into two or three main boughs near
> the top, with long **drooping branches** hanging down toward the ground
> — the silhouette readers will recognize instantly as a willow. Leaves
> are stylised, geometric, almost low-poly — strands of small leaf
> clusters falling along each drooping branch, not a single solid mass.
>
> Beside the base of the trunk, on the ground, lies one small
> **cleanly-cut branch** with a few leaves still attached, and a tiny
> pair of bonsai-style **pruning shears**, signalling that something has
> just been pruned away. The shears are the second-most prominent
> element after the tree itself.
>
> **Style:** flat vector, two-tone with a single accent color. Primary
> palette: deep forest green (`#1F6F3E`) for the foliage, warm
> terracotta (`#C46A3F`) reserved for a small flat stone or low planter
> at the base (no big pot), soft charcoal (`#1F2933`) for outlines, the
> trunk's secondary detail, and the shears. Background: transparent /
> pure white. Crisp 2–3 px strokes, consistent line weight, gentle
> rounded corners. The composition fits comfortably inside a square
> with healthy padding — readable at 32×32 px as a favicon and at
> 512×512 as a NuGet package icon.
>
> **Avoid:** photoreal rendering, gradients, drop shadows, text or
> wordmarks, sakura blossoms, glow effects, isometric perspective, busy
> backgrounds, more than three colors, water (no pond beneath the
> tree — keep the focus on the tree and shears).
>
> Output: 1024×1024 transparent PNG, centered, with at least 10%
> padding on all sides.

## Variants to try after the first generation

1. **Wordmark version:** the willow mark to the left of the word
   `Willow` in a geometric sans-serif (Inter, Geist, or similar) set
   in `#1F2933`, with the foliage color used as a single accent on the
   crossbar of the `W` or the dot of the `i`.
2. **Monochrome version:** all charcoal `#1F2933` on transparent, no
   terracotta, no accent — for embedding on dark/light backgrounds
   where the color version would clash.
3. **Favicon crop:** just the upper canopy + the drooping branches in
   a tight 32×32, optimised for browser tabs.
4. **No-shears variant:** the same composition but without the
   pruning shears or cut branch, in case the metaphor reads too
   on-the-nose.
5. **Reuse-from-bonsai variant:** if you have the existing bonsai PNG
   you want to keep visual continuity with, hand it to Nano Banana and
   ask: "Reshape the tree into a weeping willow with the same leaf
   geometry and color palette, keep the cut branch and shears."

## Notes on iteration

- If the drooping branches look thready / wispy, ask for "thicker,
  more deliberate strands — geometric beads of leaves, not strings".
- If the willow's silhouette reads as a generic shrub, ask for "the
  trunk visibly extending up into the canopy, classic willow profile
  with the foliage cascading down".
- If the shears get lost, ask Nano Banana to make them "slightly
  larger and place them at 5 o'clock from the trunk on a clear patch
  of ground".
- If foliage looks too uniform, ask for "two clusters of denser
  leaves where the wind hasn't moved them, sparser strands in
  between".

## Working with the result

Save the final 1024×1024 PNG to `src/icon.png` (replacing the
inherited mark). The `.csproj` is already configured to pack
`icon.png` as the NuGet package icon. Update the README's
[`Logo`](../README.md#logo) section if the description deserves a
tweak after seeing the final art.
