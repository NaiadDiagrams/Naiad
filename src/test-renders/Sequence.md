# Sequence

## Simple

**Input:**
```
sequenceDiagram
    Alice->>Bob: Hello Bob
    Bob-->>Alice: Hi Alice
```
```mermaid
sequenceDiagram
    Alice->>Bob: Hello Bob
    Bob-->>Alice: Hi Alice
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgQWxpY2UtXHUwMDNFXHUwMDNFQm9iOiBIZWxsbyBCb2JcbiAgICBCb2ItLVx1MDAzRVx1MDAzRUFsaWNlOiBIaSBBbGljZSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![Simple](../Tests/Sequence/SequenceTests.Simple.verified.png)

## Participants

**Input:**
```
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello
    B->>A: Hi
```
```mermaid
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello
    B->>A: Hi
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgcGFydGljaXBhbnQgQSBhcyBBbGljZVxuICAgIHBhcnRpY2lwYW50IEIgYXMgQm9iXG4gICAgQS1cdTAwM0VcdTAwM0VCOiBIZWxsb1xuICAgIEItXHUwMDNFXHUwMDNFQTogSGkiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![Participants](../Tests/Sequence/SequenceTests.Participants.verified.png)

## Actors

**Input:**
```
sequenceDiagram
    actor User
    participant Server
    User->>Server: Request
    Server-->>User: Response
```
```mermaid
sequenceDiagram
    actor User
    participant Server
    User->>Server: Request
    Server-->>User: Response
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgYWN0b3IgVXNlclxuICAgIHBhcnRpY2lwYW50IFNlcnZlclxuICAgIFVzZXItXHUwMDNFXHUwMDNFU2VydmVyOiBSZXF1ZXN0XG4gICAgU2VydmVyLS1cdTAwM0VcdTAwM0VVc2VyOiBSZXNwb25zZSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![Actors](../Tests/Sequence/SequenceTests.Actors.verified.png)

## Activation

**Input:**
```
sequenceDiagram
    Alice->>+Bob: Hello
    Bob-->>-Alice: Hi
```
```mermaid
sequenceDiagram
    Alice->>+Bob: Hello
    Bob-->>-Alice: Hi
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgQWxpY2UtXHUwMDNFXHUwMDNFXHUwMDJCQm9iOiBIZWxsb1xuICAgIEJvYi0tXHUwMDNFXHUwMDNFLUFsaWNlOiBIaSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Output:**

![Activation](../Tests/Sequence/SequenceTests.Activation.verified.png)

## Notes

**Input:**
```
sequenceDiagram
    Alice->>Bob: Hello
    Note right of Bob: Bob thinks
    Bob-->>Alice: Hi
    Note over Alice,Bob: Conversation
```
```mermaid
sequenceDiagram
    Alice->>Bob: Hello
    Note right of Bob: Bob thinks
    Bob-->>Alice: Hi
    Note over Alice,Bob: Conversation
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgQWxpY2UtXHUwMDNFXHUwMDNFQm9iOiBIZWxsb1xuICAgIE5vdGUgcmlnaHQgb2YgQm9iOiBCb2IgdGhpbmtzXG4gICAgQm9iLS1cdTAwM0VcdTAwM0VBbGljZTogSGlcbiAgICBOb3RlIG92ZXIgQWxpY2UsQm9iOiBDb252ZXJzYXRpb24iLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![Notes](../Tests/Sequence/SequenceTests.Notes.verified.png)

## AutoNumber

**Input:**
```
sequenceDiagram
    autonumber
    Alice->>Bob: Hello
    Bob->>Alice: Hi
    Alice->>Bob: How are you?
```
```mermaid
sequenceDiagram
    autonumber
    Alice->>Bob: Hello
    Bob->>Alice: Hi
    Alice->>Bob: How are you?
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgYXV0b251bWJlclxuICAgIEFsaWNlLVx1MDAzRVx1MDAzRUJvYjogSGVsbG9cbiAgICBCb2ItXHUwMDNFXHUwMDNFQWxpY2U6IEhpXG4gICAgQWxpY2UtXHUwMDNFXHUwMDNFQm9iOiBIb3cgYXJlIHlvdT8iLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![AutoNumber](../Tests/Sequence/SequenceTests.AutoNumber.verified.png)

## DifferentArrows

**Input:**
```
sequenceDiagram
    A->>B: Solid arrow
    A-->>B: Dotted arrow
    A->B: Solid open
    A-->B: Dotted open
    A-xB: Solid cross
    A--xB: Dotted cross
```
```mermaid
sequenceDiagram
    A->>B: Solid arrow
    A-->>B: Dotted arrow
    A->B: Solid open
    A-->B: Dotted open
    A-xB: Solid cross
    A--xB: Dotted cross
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgQS1cdTAwM0VcdTAwM0VCOiBTb2xpZCBhcnJvd1xuICAgIEEtLVx1MDAzRVx1MDAzRUI6IERvdHRlZCBhcnJvd1xuICAgIEEtXHUwMDNFQjogU29saWQgb3BlblxuICAgIEEtLVx1MDAzRUI6IERvdHRlZCBvcGVuXG4gICAgQS14QjogU29saWQgY3Jvc3NcbiAgICBBLS14QjogRG90dGVkIGNyb3NzIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Output:**

![DifferentArrows](../Tests/Sequence/SequenceTests.DifferentArrows.verified.png)

## Title

**Input:**
```
sequenceDiagram
    title Authentication Flow
    Client->>Server: Login request
    Server-->>Client: Token
```
```mermaid
sequenceDiagram
    title Authentication Flow
    Client->>Server: Login request
    Server-->>Client: Token
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgdGl0bGUgQXV0aGVudGljYXRpb24gRmxvd1xuICAgIENsaWVudC1cdTAwM0VcdTAwM0VTZXJ2ZXI6IExvZ2luIHJlcXVlc3RcbiAgICBTZXJ2ZXItLVx1MDAzRVx1MDAzRUNsaWVudDogVG9rZW4iLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Output:**

![Title](../Tests/Sequence/SequenceTests.Title.verified.png)

## Complex

**Input:**
```
sequenceDiagram
    title Complete Authentication Flow
    autonumber

    actor User
    participant Client as Web Client
    participant Auth as Auth Service
    participant DB as Database
    participant Email as Email Service

    User->>+Client: Enter credentials
    Client->>+Auth: POST /login
    Auth->>+DB: Query user
    DB-->>-Auth: User data
    Note right of Auth: Validate credentials
    Auth->>Auth: Generate JWT
    Note right of Auth: Token expires in 24h
    Auth-->>-Client: 200 OK + Token
    Client->>+Email: Send welcome email
    Email-->>-Client: Email sent
    Client-->>-User: Show dashboard
    Note over User,DB: Session established
```
```mermaid
sequenceDiagram
    title Complete Authentication Flow
    autonumber

    actor User
    participant Client as Web Client
    participant Auth as Auth Service
    participant DB as Database
    participant Email as Email Service

    User->>+Client: Enter credentials
    Client->>+Auth: POST /login
    Auth->>+DB: Query user
    DB-->>-Auth: User data
    Note right of Auth: Validate credentials
    Auth->>Auth: Generate JWT
    Note right of Auth: Token expires in 24h
    Auth-->>-Client: 200 OK + Token
    Client->>+Email: Send welcome email
    Email-->>-Client: Email sent
    Client-->>-User: Show dashboard
    Note over User,DB: Session established
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoic2VxdWVuY2VEaWFncmFtXG4gICAgdGl0bGUgQ29tcGxldGUgQXV0aGVudGljYXRpb24gRmxvd1xuICAgIGF1dG9udW1iZXJcblxuICAgIGFjdG9yIFVzZXJcbiAgICBwYXJ0aWNpcGFudCBDbGllbnQgYXMgV2ViIENsaWVudFxuICAgIHBhcnRpY2lwYW50IEF1dGggYXMgQXV0aCBTZXJ2aWNlXG4gICAgcGFydGljaXBhbnQgREIgYXMgRGF0YWJhc2VcbiAgICBwYXJ0aWNpcGFudCBFbWFpbCBhcyBFbWFpbCBTZXJ2aWNlXG5cbiAgICBVc2VyLVx1MDAzRVx1MDAzRVx1MDAyQkNsaWVudDogRW50ZXIgY3JlZGVudGlhbHNcbiAgICBDbGllbnQtXHUwMDNFXHUwMDNFXHUwMDJCQXV0aDogUE9TVCAvbG9naW5cbiAgICBBdXRoLVx1MDAzRVx1MDAzRVx1MDAyQkRCOiBRdWVyeSB1c2VyXG4gICAgREItLVx1MDAzRVx1MDAzRS1BdXRoOiBVc2VyIGRhdGFcbiAgICBOb3RlIHJpZ2h0IG9mIEF1dGg6IFZhbGlkYXRlIGNyZWRlbnRpYWxzXG4gICAgQXV0aC1cdTAwM0VcdTAwM0VBdXRoOiBHZW5lcmF0ZSBKV1RcbiAgICBOb3RlIHJpZ2h0IG9mIEF1dGg6IFRva2VuIGV4cGlyZXMgaW4gMjRoXG4gICAgQXV0aC0tXHUwMDNFXHUwMDNFLUNsaWVudDogMjAwIE9LIFx1MDAyQiBUb2tlblxuICAgIENsaWVudC1cdTAwM0VcdTAwM0VcdTAwMkJFbWFpbDogU2VuZCB3ZWxjb21lIGVtYWlsXG4gICAgRW1haWwtLVx1MDAzRVx1MDAzRS1DbGllbnQ6IEVtYWlsIHNlbnRcbiAgICBDbGllbnQtLVx1MDAzRVx1MDAzRS1Vc2VyOiBTaG93IGRhc2hib2FyZFxuICAgIE5vdGUgb3ZlciBVc2VyLERCOiBTZXNzaW9uIGVzdGFibGlzaGVkIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Output:**

![Complex](../Tests/Sequence/SequenceTests.Complex.verified.png)

