# Mindmap

## Simple

**Input:**
```
mindmap
  Root
    Branch A
    Branch B
    Branch C
```
```mermaid
mindmap
  Root
    Branch A
    Branch B
    Branch C
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICBSb290XG4gICAgQnJhbmNoIEFcbiAgICBCcmFuY2ggQlxuICAgIEJyYW5jaCBDIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Output:**

![Simple](../Tests/Mindmap/MindmapTests.Simple.verified.png)

## Nested

**Input:**
```
mindmap
  Root
    Branch 1
      Sub 1.1
      Sub 1.2
    Branch 2
      Sub 2.1
```
```mermaid
mindmap
  Root
    Branch 1
      Sub 1.1
      Sub 1.2
    Branch 2
      Sub 2.1
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICBSb290XG4gICAgQnJhbmNoIDFcbiAgICAgIFN1YiAxLjFcbiAgICAgIFN1YiAxLjJcbiAgICBCcmFuY2ggMlxuICAgICAgU3ViIDIuMSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![Nested](../Tests/Mindmap/MindmapTests.Nested.verified.png)

## CircleShape

**Input:**
```
mindmap
  ((Central))
    Child 1
    Child 2
```
```mermaid
mindmap
  ((Central))
    Child 1
    Child 2
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICAoKENlbnRyYWwpKVxuICAgIENoaWxkIDFcbiAgICBDaGlsZCAyIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Output:**

![CircleShape](../Tests/Mindmap/MindmapTests.CircleShape.verified.png)

## SquareShape

**Input:**
```
mindmap
  [Square Root]
    [Square Child]
    Normal Child
```
```mermaid
mindmap
  [Square Root]
    [Square Child]
    Normal Child
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICBbU3F1YXJlIFJvb3RdXG4gICAgW1NxdWFyZSBDaGlsZF1cbiAgICBOb3JtYWwgQ2hpbGQiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![SquareShape](../Tests/Mindmap/MindmapTests.SquareShape.verified.png)

## RoundedShape

**Input:**
```
mindmap
  (Rounded Root)
    (Rounded Child)
    Normal Child
```
```mermaid
mindmap
  (Rounded Root)
    (Rounded Child)
    Normal Child
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICAoUm91bmRlZCBSb290KVxuICAgIChSb3VuZGVkIENoaWxkKVxuICAgIE5vcm1hbCBDaGlsZCIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![RoundedShape](../Tests/Mindmap/MindmapTests.RoundedShape.verified.png)

## HexagonShape

**Input:**
```
mindmap
  {{Hexagon}}
    Child A
    Child B
```
```mermaid
mindmap
  {{Hexagon}}
    Child A
    Child B
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICB7e0hleGFnb259fVxuICAgIENoaWxkIEFcbiAgICBDaGlsZCBCIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Output:**

![HexagonShape](../Tests/Mindmap/MindmapTests.HexagonShape.verified.png)

## MixedShapes

**Input:**
```
mindmap
  ((Center))
    [Square]
      Normal
    (Rounded)
      {{Hex}}
```
```mermaid
mindmap
  ((Center))
    [Square]
      Normal
    (Rounded)
      {{Hex}}
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICAoKENlbnRlcikpXG4gICAgW1NxdWFyZV1cbiAgICAgIE5vcm1hbFxuICAgIChSb3VuZGVkKVxuICAgICAge3tIZXh9fSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![MixedShapes](../Tests/Mindmap/MindmapTests.MixedShapes.verified.png)

## DeepHierarchy

**Input:**
```
mindmap
  Root
    Level 1
      Level 2
        Level 3
          Level 4
            Level 5
```
```mermaid
mindmap
  Root
    Level 1
      Level 2
        Level 3
          Level 4
            Level 5
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICBSb290XG4gICAgTGV2ZWwgMVxuICAgICAgTGV2ZWwgMlxuICAgICAgICBMZXZlbCAzXG4gICAgICAgICAgTGV2ZWwgNFxuICAgICAgICAgICAgTGV2ZWwgNSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![DeepHierarchy](../Tests/Mindmap/MindmapTests.DeepHierarchy.verified.png)

## WideTree

**Input:**
```
mindmap
  Center
    A
    B
    C
    D
    E
    F
```
```mermaid
mindmap
  Center
    A
    B
    C
    D
    E
    F
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICBDZW50ZXJcbiAgICBBXG4gICAgQlxuICAgIENcbiAgICBEXG4gICAgRVxuICAgIEYiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![WideTree](../Tests/Mindmap/MindmapTests.WideTree.verified.png)

## Complex

**Input:**
```
mindmap
  ((Project))
    [Planning]
      Requirements
      Design
    [Development]
      Frontend
      Backend
      Database
    [Testing]
      Unit Tests
      Integration
    [Deployment]
      Staging
      Production
```
```mermaid
mindmap
  ((Project))
    [Planning]
      Requirements
      Design
    [Development]
      Frontend
      Backend
      Database
    [Testing]
      Unit Tests
      Integration
    [Deployment]
      Staging
      Production
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoibWluZG1hcFxuICAoKFByb2plY3QpKVxuICAgIFtQbGFubmluZ11cbiAgICAgIFJlcXVpcmVtZW50c1xuICAgICAgRGVzaWduXG4gICAgW0RldmVsb3BtZW50XVxuICAgICAgRnJvbnRlbmRcbiAgICAgIEJhY2tlbmRcbiAgICAgIERhdGFiYXNlXG4gICAgW1Rlc3RpbmddXG4gICAgICBVbml0IFRlc3RzXG4gICAgICBJbnRlZ3JhdGlvblxuICAgIFtEZXBsb3ltZW50XVxuICAgICAgU3RhZ2luZ1xuICAgICAgUHJvZHVjdGlvbiIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![Complex](../Tests/Mindmap/MindmapTests.Complex.verified.png)

