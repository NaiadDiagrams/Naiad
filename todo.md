# Diagram correctness review — TODO

Review date: 2026-08-27. Scope: every `.verified.png`/`.verified.svg` baseline under `src/Tests/` (~190 renders across 22 diagram types plus the Skia/ImageSharp raster backends), each checked against its Mermaid input for semantic correctness (elements present, shapes/markers/directions right, data positions verified arithmetically, text legible). Items are grouped by diagram type, most broken first. `bug` = output is semantically wrong or illegible; `cosmetic` = correct but visibly off.

Note: the checked-in baselines pin the buggy output, so every fix below requires re-accepting the affected `.verified.svg` files and re-running `PngRegenerator` (and the fixes themselves invalidate the corresponding `src/test-renders/` images).

**Status:** every item is fixed and its baselines re-accepted (597 tests green), bar one left deliberately: the C4 duplicate-element-id cosmetic, whose *visible* output already matches Mermaid.

## Class — FIXED (2026-08-27)

- [x] **bug** Class body members are never rendered. `+String name` / `+int age` (`ClassTests.Members`), `+makeSound()` / `+move() : void` (`Methods`), and all four in `MembersAndMethods` are silently dropped — the render is a bare one-compartment name box (SVG contains one rect + one text). Should be the three-compartment box with attribute and method sections. Same root cause drops the `<<interface>>` stereotype and method in `InterfaceAnnotation`.
  - *Cause*: the class-body parser's blank-line branch consumed the indent before the closing `}` and then failed, so `Many()` failed mid-body; the whole `{ … }` was backtracked away as an optional body and the remaining lines were left unparsed (Pidgin does not require EOF). Fixed by wrapping that branch in `Try` and ending it on a newline rather than `LineEnd`.
- [x] **bug** `ClassTests.Complex` renders a single box: of 9 declared classes and 6 relationships, only "IRepository" appears — everything after (or while) parsing the first class with a `~T~` generic parameter is lost, and the generic marker itself (`IRepository~T~` → `IRepository<T>`) is not rendered.
  - *Cause*: the identifier parser had no notion of `~T~`, so parsing stopped dead at the first generic and silently discarded the rest of the document. Class names, member types and relationship endpoints now accept a `~…~` argument; the class is keyed on its bare name and displays as `IRepository<T>` / `List<Item>`. Also fixed along the way: `name: Type` parameters, the trailing `$`/`*` classifier, space-separated return types (`+getId() int`), and cardinality in Mermaid's position (`User "1" --> "1..*" Address`).
- [x] **bug** Relationship markers attach to the wrong end. `Animal <|-- Dog` puts the hollow inheritance triangle on Dog/Cat (subclass) instead of Animal — semantics inverted. Same end-swap for composition `Car *-- Engine` and aggregation `Library o-- Book`.
  - *Cause*: `RelationshipType` recorded only the *kind* of relationship, not which end carried the glyph, and the renderer always drew it at the target. Each end now carries its own `RelationshipMarker`, parsed from the token on that side of the line, so `<|--`/`--|>`, `*--`/`--*`, `o--`/`--o` and `<--`/`-->` all mark the end the author wrote. Regression test: `ClassTests.ReversedRelationships`.
- [x] **bug** (found while fixing the above) From-cardinality labels were positioned by a fixed `-10` Y offset, which put them *inside* the source class box; since boxes paint after edges, every `"1"` in `Complex` was invisible. Labels now step along the edge direction, clear of both the border and the marker.

## Sequence — FIXED (2026-08-27)

- [x] **bug** Deactivation applies to the wrong participant: the `-` suffix is treated as deactivating the message *receiver*; Mermaid semantics deactivate the *sender*. `Bob-->>-Alice: Hi` therefore tries to deactivate Alice and Bob's activation bar runs to the diagram bottom (`SequenceTests.Activation`). In `Complex`, four bars end at the wrong message or never end.
  - *Fixed*: `-` now closes `msg.FromId`. All four `Complex` bars land on the right message (DB 3→4, Auth 2→6, Email 7→8, Client 1→9).
- [x] **bug** Activation rects are drawn after (on top of) messages and notes, so a long bar paints over note and label text. Bars belong beneath text.
  - *Fixed*: activation spans are now computed in their own pass (`CalculateActivations`) and painted before the messages and notes, so the bars still cover the lifeline but sit under everything that crosses them.
- [x] **bug** `Note right of Bob` overflows the canvas: note box at x=280–400 in a 290-wide viewBox — the text is entirely outside the visible area (`SequenceTests.Notes`).
  - *Fixed*: the canvas is now sized around everything hanging off the participants — notes on both sides and self-message loops. A note that hangs off the *left* edge shifts the whole diagram right instead of being clipped (`SequenceTests.NoteLeftOfFirstParticipant`).
- [x] **bug** `Note over A,B` does not span the named participants: rendered as a fixed 120-unit box centered between them, so it reads as a note over the wrong participants.
  - *Fixed*: an "over" note spans from the outer edge of one named participant to the other.
- [x] **bug** The `actor` stick figure is malformed: legs are drawn upward-and-outward from the bottom of the body line, producing a circle-with-chevron glyph whose vertex collides with the participant label.
  - *Cause*: the body already ended 5px past the bottom of the participant band, so the legs ran *upward* to reach it. The figure is now scaled to the band with the legs splaying down and out, and diagrams containing an actor get a taller header so the name sits clear of the lifelines.
- [x] **cosmetic** Notes are fixed-width (120 units); longer text touches/overflows the box border.
  - *Fixed*: notes grow to fit their text, with 120 as the minimum.
- [x] **bug** (found while fixing the above) A standalone `activate X` / `deactivate X` line takes no vertical space, so it inherited the *next* message's slot: `activate Bob` after a message started the bar one message too low and a trailing `deactivate` ran past the last message off the bottom of the diagram. These lines now bind to the message above them, so the explicit form renders identically to `+`/`-`. Regression test: `SequenceTests.ExplicitActivation`.

## Requirement — FIXED (2026-08-27)

- [x] **bug** Declared attributes are silently dropped from requirement/element boxes in all 6 tests: `id:`, `verifymethod:`, and `docref:` never appear, `text:` is truncated with an ellipsis ("The system shall do so…"), and `risk:` is reduced to an unlabeled colored dot.
  - *Fixed*: boxes now carry a header (type + name), a separator, and one row per declared attribute — `Id:` / `Text:` / `Risk:` / `Verification:` for requirements, `Type:` / `Doc Ref:` for elements — and are sized to their content instead of truncating into a fixed 180×80 box.
- [x] **bug** `AllTypes`: req1/req2 declare no risk yet display an orange (medium) risk dot — the render asserts a risk level the input never stated.
  - *Cause*: `Risk` and `VerifyMethod` were non-nullable with defaults of Medium and Test, so an undeclared value was indistinguishable from a declared one. Both are now nullable and only render when the diagram says so. The unlabeled risk dot is gone — the `Risk:` row states it explicitly.
- [x] **cosmetic** Non-`contains` relationships (satisfies/derives/verifies/…) are solid; Mermaid draws them dashed (dasharray 10,7).
- [x] **cosmetic** Diagonal edges clip to a radius around node centers instead of the rectangle borders (`AllTypes`: the `<<verifies>>` edge starts inside elem1's fill and its arrowhead lands inside req2).
  - *Cause*: scaling both axes by the half-extents traces the inscribed *ellipse*, which is inside the box everywhere except the four edge midpoints. Edges now clip to the rectangle by scaling to the nearer axis limit.
- [x] **cosmetic** Type headers abbreviate Mermaid's names (`<<Functional>>` vs `<<Functional Requirement>>`).
- [x] **cosmetic** Canvas much larger than content (`Simple`/`Functional`: viewBox 520×180 with content ~220×120 — right half empty).
  - *Fixed*: the canvas is measured from the laid-out columns (and the title, which could previously overflow it), so the second column is no longer reserved when there are no elements.
- [x] **cosmetic** (found while fixing the above) Relation labels sat on top of their line. The label is now pushed along the line's perpendicular by half its own width or height depending on the edge's direction, and always to the upper side regardless of which way the relation was declared.

## GitGraph — FIXED (2026-08-27)

- [x] **bug** Commit id labels are white text centered *on* the r=12 commit circle; anything wider than ~24px overhangs onto the white background and the overhanging glyphs vanish — every auto-id renders as "ommit0" / "erge3" / "herry2" (9 of 11 tests).
  - *Fixed*: captions are now dark text on a grey chip below the commit. Lane spacing grows to the widest caption so neighbouring chips can't run together, and the canvas reserves what the captions below and the tags above actually need — a tag on the first lane previously sat inside the padding.
- [x] **bug** `type: REVERSE` renders as a plain white-filled circle with no cross glyph — the type is not communicated, and its white label on the white fill is completely invisible (`Types`).
  - *Fixed*: REVERSE is a crossed circle. (The invisible label went with the caption move.)
- [x] **bug** Cherry-pick commits lose the source reference: rendered as an ordinary commit with the meaningless auto-id "cherry2" (clipped) instead of Mermaid's "cherry-pick:two" label + cherry glyph (`CherryPick`).
  - *Fixed*: `GitCommit.Label` carries a caption distinct from the id, so a cherry-pick is captioned `cherry-pick:two` while keeping a unique key, and it draws as a pair of cherries.
- [x] **cosmetic** Merge commits look identical to normal commits (Mermaid uses a distinct double-circle); HIGHLIGHT renders as a yellow circle rather than Mermaid's squarish highlight.
  - *Fixed*: merges are double circles and HIGHLIGHT is a block. How a commit came about (`IsMerge` / `IsCherryPick`) picks the glyph and its declared `type:` picks the fill, so a merge still reads as a merge whatever type it was given.

## EntityRelationship — FIXED (2026-08-27)

- [x] **bug** Cardinality marker groups are mirrored along the edge in every many/zero end: the crow's-foot fork points *away* from the entity (reads as an arrowhead) and the min-cardinality glyph (circle/bar) sits nearest the box. Correct order: fork/bar (max) adjacent to and touching the entity, circle/bar (min) farther out.
  - *Cause*: every mark was placed by stepping *outwards* from a base 15px off the border, in declaration order — so the min glyph was always laid down first, nearest the entity, and the foot was drawn with its apex at the near end fanning outwards, which reads as an arrowhead. Marks are now positioned by an explicit distance from the border with the max-cardinality glyph against it, and the foot's three prongs converge at an apex away from the entity and fan out to meet the border.
- [x] **cosmetic** Quote delimiters are rendered literally: attribute comments show `int id "Primary key"` (`Comments`) and quoted relationship labels show `"ships to"` (`Compelx`).
  - *Cause*: two different ones. The parser already stripped an attribute comment's quotes and the *renderer* put them back when it concatenated the row; relationship labels were taken as the raw rest of the line, quotes included, so the parser now reads a quoted string where there is one. The comment is drawn as its own lighter column rather than concatenated, since without the quotes it would otherwise run straight on from the attribute name.
- [x] **bug** (found while fixing the above) Entity width omitted the 20px PK/FK/UK gutter that the attribute text is actually indented by, so the longest row overflowed the box — visible as soon as comments became a separate column.

## Timeline — FIXED (2026-08-27)

- [x] **bug** Period spacing is fixed at 120 units while event-box width is text-driven, so adjacent event boxes overlap (`Title`, `TextPeriods`), and in `MultipleSections` the "Industrial Revolution" box starts left of its own section band, painting over the neighbouring one.
  - *Cause*: every period got the same 120-unit slot regardless of what was drawn under it, and event boxes are sized to their text and centred on the period, so anything wider than 120 spilled both ways. Each period is now as wide as its own widest event (or its label) plus a gap, and section widths and the canvas follow from that — so a box can no longer reach its neighbour or escape its band. This also removes an `IndexOf` per period, which was both quadratic and wrong for two periods sharing a label.

## Sankey — FIXED (2026-08-27)

- [x] **bug** Left-column node labels are clipped off the canvas: anchored `text-anchor="end"` at x=15, so "Input"→"ut", "Source"→"rce", "Coal"→"al", "Gas"→"as", "Nuclear"→"ear" (`SingleLink`, `ThreeColumns`, `EnergyFlow`).
  - *Cause*: the label side keyed off "is this the last column?", which put every other column's label to the *left* — fine in the gaps between columns, off the canvas for column 0. It now keys off which half of the plot the node is in, as Mermaid does, so a label always runs into the diagram rather than off its edge. The 100px right margin that existed to hold the last column's labels is no longer needed.
- [x] **bug** Canvas height scales with raw data magnitude: `chartHeight = Math.Max(300, totalValue * 2)` turns `BudgetFlow` (totals 4000) into a 740×8040 SVG with microscopic labels.
  - *Fixed*: the plot is a fixed 400px that values scale into, so the same diagram is the same size whether its numbers are in units or thousands. Only the busiest column's node count can push it taller, so stacked bars can't collapse below their minimum height. BudgetFlow is now 460×440.
- [x] **bug** Node values are never displayed; Mermaid's sankey-beta defaults to `showValues: true`.
  - *Fixed*: the value is drawn under the node name.
- [x] **cosmetic** (found while fixing the above) Ribbons were fully opaque, so the labels now sitting over them were hard to read — "Nuclear 30" in grey on crimson — and crossing ribbons merged into one shape. They are translucent now, as in Mermaid, which fixes both.

## C4 — FIXED (2026-08-27)

- [x] **bug** `Rel_L` / `Rel_R` layout hints are ignored (`DirectionalRelationships`): "Left Service" lands directly below Core, "Right Service" lands below-*left*, and "Downstream" is pushed below-right.
  - *Cause*: `Edge.RankConstraint` is written by the C4 renderer and **never read by anything** — the layout engine has no notion of rank constraints, so Left/Right/Neighbor had no effect on placement at all. Rather than teach the shared Dagre engine about constraints, the targets are now placed against their source after layout (`ApplyPositionalDirections`), which is where C4 already special-cased these relationships for *drawing*. `Rel_D` is aligned under its source too, since the rank ordering could otherwise leave it off to one side. The four keywords now render as a compass around the source.
  - The `RankConstraint` assignments are left in place (removing them would leave the shared `Edge` property with no producers) but the call site now says plainly that nothing reads them.
  - Still open: boundary layouts use a separate nested-layout path that does not apply positional directions. No test covers `Rel_L`/`Rel_R` inside a boundary.
- [x] **bug** Element descriptions are truncated with an ellipsis instead of wrapped ("Allows customers to…", "External email prov…", etc. across `External`, `Complex`, `Boundaries`, `DuplicateElementIds`).
  - *Fixed*: descriptions word-wrap to the box width and the box grows by line count, via the existing `ContentLineCount`/`NodeHeight` sizing. A word longer than the line is broken rather than allowed to overflow.
- [x] **cosmetic** Boundary titles are centered at the top edge, exactly where vertical edges enter, so the customer→web edge strikes through the "Internet Banking [System]" caption.
  - *Fixed*: two causes. Captions now sit top-left as Mermaid places them, and they are drawn *after* the edges rather than before — boundary boxes have to be painted first so their fills don't cover nested content, which was also putting their captions under every edge.
- [ ] **cosmetic** Duplicate element ids draw both boxes at identical coordinates — the first ("Web App") is completely hidden under the second ("Duplicate"). Left as-is: the *visible* result already matches Mermaid, which merges duplicate aliases with the last declaration winning. Deduplicating in the model would remove the invisible box but change nothing on screen.

## Naiad.ImageSharp backend — FIXED (2026-08-27)

- [x] **bug** Every stroke renders at exactly 2× the intended width at `Png.Scale=2`: `ImageSharpSurface.StrokePath` passed `width * Scale(transform)` to the pen while `DrawingOptions.Transform` applies the same transform to the stroked outline — the scale hit the width twice.
  - *Fixed*: the pen width stays in path units and the transform scales the outline, as it already did for geometry and text. Verified by measurement rather than eye: the class-box border is now 2px in both backends, where ImageSharp was 4px against Skia's 2px. Dash lengths were unaffected either way, since `ToPen` normalises the pattern against whatever width it is handed. `Scale` had no other caller and is gone. All 7 `ImageSharp*` baselines re-accepted.

## Flowchart — FIXED (2026-08-28)

- [x] **bug** Asymmetric node `>text]` has mirrored left-edge geometry: Naiad draws a convex point protruding left (arrow-tip silhouette); Mermaid's `rect_left_inv_arrow` has protruding top/bottom-left corners with an inset mid-left vertex (concave notch). `ShapePathGenerator.Asymmetric`, `src/Naiad/Rendering/ShapePathGenerator.cs:153` (`ComplexPipeline`).
  - *Fixed*: the corners are now the leftmost points and the mid-left vertex sits back between them, so the left edge reads as a notch cut into the box. The notch depth is Mermaid's `height / 2` rather than a share of the width, and the node grows by that depth on both sides so the centred label keeps Mermaid's 15-unit clearance from the vertex.
- [x] **bug** Subroutine `[[...]]` inner bars collide with the label: bars are inset 10% of node width but the label spans the full width, so the bars cut through glyphs ("**4**29 Too Many Request**s**"). Size the node so label + padding fits between the bars (`ShapePathGenerator.Subroutine`; `ComplexPipeline`, milder in `FullFeaturedSyntax`).
  - *Fixed*: the bars are a fixed 8 units in (Mermaid's inset) instead of a share of the width — the old proportional inset pushed the bars *further* across the label the wider the node got — and the node is widened by that inset on both sides so the label sits between them.
- [x] **cosmetic** (part) Subgraph titles were painted before the edges, so an edge crossing into a subgraph drew over the title and left it unreadable (`ComplexPipeline`'s "Resilience layer", both titles in `NestedSubgraphs`). Titles now draw last, as Mermaid does, so the text stays legible where an edge crosses its band.
- [x] **cosmetic** Edges still route through unrelated node bodies: `SVCA <--> PG` and `SVCC <--> PG` cross the "Transactional outbox" cylinder, and edges still cross subgraph title bands (they are merely no longer drawn over the text).
  - *Fixed (nodes)*: `EdgeObstacleRouter`, run over the routed points in `DagreEngine`. Dagre ranks nodes so edges cross the gaps between ranks, but aims an edge's **end segment** straight at the target's border without checking what is in between; when the target shares a rank with a neighbour lying between it and the incoming segment, that segment cuts across the neighbour. A blocked end segment is now re-aimed into an L - along the incoming direction until it is over the target, then straight in - and the target is entered through whichever border that approach reaches. Only a segment that actually crosses a node is touched, and only when both legs of the replacement are themselves clear, so an edge that cannot be improved is left exactly as dagre routed it. A scan of every baseline for edge-through-node crossings goes from 2 to 0, and `ComplexPipeline` is the only render that changed.
  - *Masked, not rerouted (titles)*: an edge entering a subgraph has to cross the title band, and it lands on the title itself whenever the node it targets is the one the box is centred on - so there is no route around, only a question of what is drawn on top. The title now gets a backing rect in the box's own fill: invisible against the box, and the edge passes behind it. The line still geometrically crosses the band in all 6 cases; it is no longer visible through the glyphs.

## Block — FIXED (2026-08-27)

- [x] **bug** Rounded `b("Rounded")` and stadium `c(["Stadium"])` are indistinguishable: both emit `rx=20` on a 40-high rect (i.e. both are stadiums).
  - *Cause*: `case BlockShape.Rounded:` fell straight through into `case BlockShape.Stadium:`, so both drew with `rx = height / 2`. Rounded has its own case and a small corner radius now.
- [x] **bug** Circle `d(("Circle"))` is a fixed r=20 circle not sized to its label — the text already touches the stroke on both sides, and any longer label overflows the shape.
  - *Cause*: the circle is inscribed in its grid cell (`Math.Min(width, height) / 2`), and the grid was a fixed 120x60, so the radius could never exceed 20 whatever the label. The grid now grows to the diameter the largest circle label needs, which leaves the inscribed-circle rule intact and correct.

## State — FIXED (2026-08-28)

- [x] **bug** `TransitionLabels`: both curved labeled edges contain a backwards retrace at the label junction — the path descends past the label, jumps back up in a straight line, then descends again (e.g. `… 35.2 218.05 L 35.2 141.95 …`), producing stray vertical slashes through the "timeout"/"reset" labels and doubled strokes. The two curve/line junction points appear swapped (compare the correct forward mid-segment in `MultipleStates`).
  - *Cause*: not swapped junctions — the corner radius was bounded only by the edge's *horizontal* run (`Math.Min(80, (startX - leftEdge) / 2)`). Each of the two quarter-circle flares consumes `2 * radius` of the vertical run, so once the radius passed a quarter of that run the second flare began above the first flare's end and the straight segment joining them ran backwards. `MultipleStates` looked correct only because its states are further apart horizontally, which happened to keep the radius small. The radius is now also bounded by `verticalRun / 4`, so the flares can meet but never cross.
- [x] **cosmetic** `Complex`: `note right of Processing` renders below-left of the state — the side keyword is not honored (`note left of Error` is correct).
  - *Cause*: the declared side was never read. Note placement was purely geometric (which half of the diagram the state sat in), then overridden to whichever side was clear of the routed-edge corridor. The declared side now decides, and a note on a side carrying a corridor is pushed out past that corridor instead of being flipped to the other side — so the keyword is honoured without a corridor line running under the note. The two places that position notes (canvas reservation and rendering) now share one `NoteX` helper rather than duplicating the arithmetic.
- [x] **cosmetic** `TransitionLabels`: the "reset" edge passes through the final-state marker's stroke ring.
  - *Fixed*: routed edges now slide their exit along the source's border, nearest the centre first, until the curve they produce clears every state parked in the corridor. `reset` leaves `Inactive` 12 units above centre and passes above the marker.
  - *Correcting the earlier note*: it claimed only ~6 units of slack were available, so no reliable rule existed. That measured the wrong thing - a flat horizontal stub at the exit height. The exit is a cubic that climbs away from the source immediately, so it is already above the marker by the time it reaches it; the check now samples the actual curve rather than approximating it as a stub, and finds comfortable clearance.

## Tooling — FIXED (2026-08-27)

- [x] **bug** `DocGeneratorTests.Generate` deletes `src/test-renders/` and rebuilds it, but extracted inputs only from inline `const string input` literals. `StateTests` calls `VerifySvg(StateSamples.Simple)`, so the generator found no State tests and **deleted `State.md` and its entry in `renders.include.md`**.
  - *Fixed*: the extractor now resolves an input passed as a shared constant by parsing the sibling `*Samples.cs` file. `State.md` survives a regeneration with all 9 sections.
- [x] Regenerating also restored the sections that were missing from the committed docs. The regenerated output is now purely additive — 435 lines added, none removed.

## Small fidelity items — FIXED (2026-08-28)

- [x] **cosmetic** Gantt: task bars display the internal task **id** ("a1", "b1") centered in the bar (`GanttRenderer.cs` ~line 263); Mermaid never shows ids — it shows the task name in/next to the bar. All 9 gantt renders are otherwise positionally exact.
  - *Fixed*: bars carry the task **name**, placed as Mermaid places it — centred inside the bar when it fits, otherwise immediately past the bar (to its right, or to its left when the chart has no room on the right), switching from white to dark text when it lands on the background. Milestones are labelled too; they previously had no label at all, so a milestone was identifiable only by its row heading.
- [x] **cosmetic** Mindmap: `(rounded)` nodes render identically to default (no-bracket) nodes; Mermaid's default style (borderless band) is distinct from the rounded rect (`RoundedShape`, visible suite-wide).
  - *Fixed*: a bracket-less node is now Mermaid's band — rounded top corners, square bottom, no border, and an underline along the bottom edge — while `(rounded)` keeps a bordered rect with Mermaid's `rx = padding`. The two syntaxes now render differently.
- [x] **cosmetic** Quadrant: point labels at x=1 clip at the viewBox edge ("Top Right"→"Top R") (`EdgePositions`). Mermaid clips identically, so lowest priority — but the text is unreadable.
  - *Fixed*: a point label that would run off either side is nudged back inside the canvas instead of being clipped. Deliberately diverges from Mermaid, which clips.

## Reviewed clean — no action

- **Architecture** (8/8), **Kanban** (5/5), **Packet** (7/7, bit spans verified), **Pie** (3/3, angles verified), **Radar** (6/6, vertex radii/angles verified), **Treemap** (6/6, areas verified), **XYChart** (7/7, bar/line values verified), **UserJourney** (8/8, faces/scores/actors verified), **Gantt** (9/9 positionally exact).
- Raster backends: apart from the ImageSharp stroke bug, the two backends match each other and the Svg.Skia reference in geometry, color, text, markers, and dashes.
- Deliberate design differences left alone: left-to-right mindmap layout (vs radial), slice/strip treemap tiling (vs squarify), grouped XYChart bar series (vs overlapped), architecture service boxes, single 0–31 packet ruler, "1." autonumber prefixes.
