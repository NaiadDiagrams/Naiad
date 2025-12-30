# Timeline

## Simple

**Input:**
```
timeline
    2020 : Event One
    2021 : Event Two
    2022 : Event Three
```
**Rendered by Mermaid:**
```mermaid
timeline
    2020 : Event One
    2021 : Event Two
    2022 : Event Three
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICAyMDIwIDogRXZlbnQgT25lXG4gICAgMjAyMSA6IEV2ZW50IFR3b1xuICAgIDIwMjIgOiBFdmVudCBUaHJlZSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![Simple](../Tests/Timeline/TimelineTests.Simple.verified.png)

## Title

**Input:**
```
timeline
    title History of Computing
    1940 : First Computer
    1970 : Personal Computers
    2000 : Internet Era
```
**Rendered by Mermaid:**
```mermaid
timeline
    title History of Computing
    1940 : First Computer
    1970 : Personal Computers
    2000 : Internet Era
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICB0aXRsZSBIaXN0b3J5IG9mIENvbXB1dGluZ1xuICAgIDE5NDAgOiBGaXJzdCBDb21wdXRlclxuICAgIDE5NzAgOiBQZXJzb25hbCBDb21wdXRlcnNcbiAgICAyMDAwIDogSW50ZXJuZXQgRXJhIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Rendered by Naiad:**

![Title](../Tests/Timeline/TimelineTests.Title.verified.png)

## MultipleEventsPerPeriod

**Input:**
```
timeline
    2004 : Facebook
         : Gmail
    2005 : YouTube
    2006 : Twitter
         : Spotify
```
**Rendered by Mermaid:**
```mermaid
timeline
    2004 : Facebook
         : Gmail
    2005 : YouTube
    2006 : Twitter
         : Spotify
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICAyMDA0IDogRmFjZWJvb2tcbiAgICAgICAgIDogR21haWxcbiAgICAyMDA1IDogWW91VHViZVxuICAgIDIwMDYgOiBUd2l0dGVyXG4gICAgICAgICA6IFNwb3RpZnkiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Rendered by Naiad:**

![MultipleEventsPerPeriod](../Tests/Timeline/TimelineTests.MultipleEventsPerPeriod.verified.png)

## Sections

**Input:**
```
timeline
    title Technology Timeline
    section Early Era
        1990 : World Wide Web
        1995 : Windows 95
    section Modern Era
        2007 : iPhone
        2010 : iPad
```
**Rendered by Mermaid:**
```mermaid
timeline
    title Technology Timeline
    section Early Era
        1990 : World Wide Web
        1995 : Windows 95
    section Modern Era
        2007 : iPhone
        2010 : iPad
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICB0aXRsZSBUZWNobm9sb2d5IFRpbWVsaW5lXG4gICAgc2VjdGlvbiBFYXJseSBFcmFcbiAgICAgICAgMTk5MCA6IFdvcmxkIFdpZGUgV2ViXG4gICAgICAgIDE5OTUgOiBXaW5kb3dzIDk1XG4gICAgc2VjdGlvbiBNb2Rlcm4gRXJhXG4gICAgICAgIDIwMDcgOiBpUGhvbmVcbiAgICAgICAgMjAxMCA6IGlQYWQiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Rendered by Naiad:**

![Sections](../Tests/Timeline/TimelineTests.Sections.verified.png)

## TextPeriods

**Input:**
```
timeline
    title Project Phases
    Planning : Define scope
    Design : Create mockups
    Development : Build features
    Testing : Quality assurance
```
**Rendered by Mermaid:**
```mermaid
timeline
    title Project Phases
    Planning : Define scope
    Design : Create mockups
    Development : Build features
    Testing : Quality assurance
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICB0aXRsZSBQcm9qZWN0IFBoYXNlc1xuICAgIFBsYW5uaW5nIDogRGVmaW5lIHNjb3BlXG4gICAgRGVzaWduIDogQ3JlYXRlIG1vY2t1cHNcbiAgICBEZXZlbG9wbWVudCA6IEJ1aWxkIGZlYXR1cmVzXG4gICAgVGVzdGluZyA6IFF1YWxpdHkgYXNzdXJhbmNlIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Rendered by Naiad:**

![TextPeriods](../Tests/Timeline/TimelineTests.TextPeriods.verified.png)

## MultipleSections

**Input:**
```
timeline
    section Ancient History
        3000 BC : Writing invented
        500 BC : Democracy
    section Medieval
        500 AD : Dark Ages
        1400 : Renaissance
    section Modern
        1800 : Industrial Revolution
        2000 : Digital Age
```
**Rendered by Mermaid:**
```mermaid
timeline
    section Ancient History
        3000 BC : Writing invented
        500 BC : Democracy
    section Medieval
        500 AD : Dark Ages
        1400 : Renaissance
    section Modern
        1800 : Industrial Revolution
        2000 : Digital Age
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICBzZWN0aW9uIEFuY2llbnQgSGlzdG9yeVxuICAgICAgICAzMDAwIEJDIDogV3JpdGluZyBpbnZlbnRlZFxuICAgICAgICA1MDAgQkMgOiBEZW1vY3JhY3lcbiAgICBzZWN0aW9uIE1lZGlldmFsXG4gICAgICAgIDUwMCBBRCA6IERhcmsgQWdlc1xuICAgICAgICAxNDAwIDogUmVuYWlzc2FuY2VcbiAgICBzZWN0aW9uIE1vZGVyblxuICAgICAgICAxODAwIDogSW5kdXN0cmlhbCBSZXZvbHV0aW9uXG4gICAgICAgIDIwMDAgOiBEaWdpdGFsIEFnZSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![MultipleSections](../Tests/Timeline/TimelineTests.MultipleSections.verified.png)

## Complex

**Input:**
```
timeline
    title Social Media Evolution
    section Web 1.0
        1997 : Six Degrees
        1999 : LiveJournal
    section Web 2.0
        2003 : MySpace
             : LinkedIn
        2004 : Facebook
        2005 : YouTube
        2006 : Twitter
    section Mobile Era
        2010 : Instagram
        2011 : Snapchat
        2016 : TikTok
```
**Rendered by Mermaid:**
```mermaid
timeline
    title Social Media Evolution
    section Web 1.0
        1997 : Six Degrees
        1999 : LiveJournal
    section Web 2.0
        2003 : MySpace
             : LinkedIn
        2004 : Facebook
        2005 : YouTube
        2006 : Twitter
    section Mobile Era
        2010 : Instagram
        2011 : Snapchat
        2016 : TikTok
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoidGltZWxpbmVcbiAgICB0aXRsZSBTb2NpYWwgTWVkaWEgRXZvbHV0aW9uXG4gICAgc2VjdGlvbiBXZWIgMS4wXG4gICAgICAgIDE5OTcgOiBTaXggRGVncmVlc1xuICAgICAgICAxOTk5IDogTGl2ZUpvdXJuYWxcbiAgICBzZWN0aW9uIFdlYiAyLjBcbiAgICAgICAgMjAwMyA6IE15U3BhY2VcbiAgICAgICAgICAgICA6IExpbmtlZEluXG4gICAgICAgIDIwMDQgOiBGYWNlYm9va1xuICAgICAgICAyMDA1IDogWW91VHViZVxuICAgICAgICAyMDA2IDogVHdpdHRlclxuICAgIHNlY3Rpb24gTW9iaWxlIEVyYVxuICAgICAgICAyMDEwIDogSW5zdGFncmFtXG4gICAgICAgIDIwMTEgOiBTbmFwY2hhdFxuICAgICAgICAyMDE2IDogVGlrVG9rIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Rendered by Naiad:**

![Complex](../Tests/Timeline/TimelineTests.Complex.verified.png)

