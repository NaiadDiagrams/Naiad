# UserJourney

## Simple

**Input:**
```
journey
    title My Working Day
    section Morning
        Make coffee: 5: Me
        Check emails: 3: Me
```
**Rendered by Mermaid:**
```mermaid
journey
    title My Working Day
    section Morning
        Make coffee: 5: Me
        Check emails: 3: Me
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIE15IFdvcmtpbmcgRGF5XG4gICAgc2VjdGlvbiBNb3JuaW5nXG4gICAgICAgIE1ha2UgY29mZmVlOiA1OiBNZVxuICAgICAgICBDaGVjayBlbWFpbHM6IDM6IE1lIiwibWVybWFpZCI6eyJ0aGVtZSI6ImRlZmF1bHQifX0=)

**Rendered by Naiad:**

![Simple](../Tests/UserJourney/UserJourneyTests.Simple.verified.png)

## MultipleSections

**Input:**
```
journey
    title Customer Journey
    section Discovery
        Visit website: 4: Customer
        Browse products: 3: Customer
    section Purchase
        Add to cart: 4: Customer
        Checkout: 2: Customer
    section Delivery
        Track order: 5: Customer
        Receive package: 5: Customer
```
**Rendered by Mermaid:**
```mermaid
journey
    title Customer Journey
    section Discovery
        Visit website: 4: Customer
        Browse products: 3: Customer
    section Purchase
        Add to cart: 4: Customer
        Checkout: 2: Customer
    section Delivery
        Track order: 5: Customer
        Receive package: 5: Customer
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIEN1c3RvbWVyIEpvdXJuZXlcbiAgICBzZWN0aW9uIERpc2NvdmVyeVxuICAgICAgICBWaXNpdCB3ZWJzaXRlOiA0OiBDdXN0b21lclxuICAgICAgICBCcm93c2UgcHJvZHVjdHM6IDM6IEN1c3RvbWVyXG4gICAgc2VjdGlvbiBQdXJjaGFzZVxuICAgICAgICBBZGQgdG8gY2FydDogNDogQ3VzdG9tZXJcbiAgICAgICAgQ2hlY2tvdXQ6IDI6IEN1c3RvbWVyXG4gICAgc2VjdGlvbiBEZWxpdmVyeVxuICAgICAgICBUcmFjayBvcmRlcjogNTogQ3VzdG9tZXJcbiAgICAgICAgUmVjZWl2ZSBwYWNrYWdlOiA1OiBDdXN0b21lciIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![MultipleSections](../Tests/UserJourney/UserJourneyTests.MultipleSections.verified.png)

## MultipleActors

**Input:**
```
journey
    title Team Collaboration
    section Planning
        Define requirements: 4: PM, Dev
        Create design: 3: Designer, PM
    section Development
        Write code: 4: Dev
        Code review: 3: Dev, Lead
    section Testing
        Test features: 4: QA, Dev
        Fix bugs: 2: Dev
```
**Rendered by Mermaid:**
```mermaid
journey
    title Team Collaboration
    section Planning
        Define requirements: 4: PM, Dev
        Create design: 3: Designer, PM
    section Development
        Write code: 4: Dev
        Code review: 3: Dev, Lead
    section Testing
        Test features: 4: QA, Dev
        Fix bugs: 2: Dev
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIFRlYW0gQ29sbGFib3JhdGlvblxuICAgIHNlY3Rpb24gUGxhbm5pbmdcbiAgICAgICAgRGVmaW5lIHJlcXVpcmVtZW50czogNDogUE0sIERldlxuICAgICAgICBDcmVhdGUgZGVzaWduOiAzOiBEZXNpZ25lciwgUE1cbiAgICBzZWN0aW9uIERldmVsb3BtZW50XG4gICAgICAgIFdyaXRlIGNvZGU6IDQ6IERldlxuICAgICAgICBDb2RlIHJldmlldzogMzogRGV2LCBMZWFkXG4gICAgc2VjdGlvbiBUZXN0aW5nXG4gICAgICAgIFRlc3QgZmVhdHVyZXM6IDQ6IFFBLCBEZXZcbiAgICAgICAgRml4IGJ1Z3M6IDI6IERldiIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![MultipleActors](../Tests/UserJourney/UserJourneyTests.MultipleActors.verified.png)

## AllScores

**Input:**
```
journey
    title Score Examples
    section Experience
        Terrible: 1: User
        Bad: 2: User
        Okay: 3: User
        Good: 4: User
        Great: 5: User
```
**Rendered by Mermaid:**
```mermaid
journey
    title Score Examples
    section Experience
        Terrible: 1: User
        Bad: 2: User
        Okay: 3: User
        Good: 4: User
        Great: 5: User
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIFNjb3JlIEV4YW1wbGVzXG4gICAgc2VjdGlvbiBFeHBlcmllbmNlXG4gICAgICAgIFRlcnJpYmxlOiAxOiBVc2VyXG4gICAgICAgIEJhZDogMjogVXNlclxuICAgICAgICBPa2F5OiAzOiBVc2VyXG4gICAgICAgIEdvb2Q6IDQ6IFVzZXJcbiAgICAgICAgR3JlYXQ6IDU6IFVzZXIiLCJtZXJtYWlkIjp7InRoZW1lIjoiZGVmYXVsdCJ9fQ==)

**Rendered by Naiad:**

![AllScores](../Tests/UserJourney/UserJourneyTests.AllScores.verified.png)

## WithoutTitle

**Input:**
```
journey
    section Tasks
        First task: 4: Alice
        Second task: 5: Bob
```
**Rendered by Mermaid:**
```mermaid
journey
    section Tasks
        First task: 4: Alice
        Second task: 5: Bob
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHNlY3Rpb24gVGFza3NcbiAgICAgICAgRmlyc3QgdGFzazogNDogQWxpY2VcbiAgICAgICAgU2Vjb25kIHRhc2s6IDU6IEJvYiIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![WithoutTitle](../Tests/UserJourney/UserJourneyTests.WithoutTitle.verified.png)

## ManyActors

**Input:**
```
journey
    title Big Team Project
    section Kickoff
        Initial meeting: 4: PM, Dev, QA, Designer, Lead, Stakeholder
    section Execution
        Development: 3: Dev, Lead
        Testing: 4: QA
```
**Rendered by Mermaid:**
```mermaid
journey
    title Big Team Project
    section Kickoff
        Initial meeting: 4: PM, Dev, QA, Designer, Lead, Stakeholder
    section Execution
        Development: 3: Dev, Lead
        Testing: 4: QA
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIEJpZyBUZWFtIFByb2plY3RcbiAgICBzZWN0aW9uIEtpY2tvZmZcbiAgICAgICAgSW5pdGlhbCBtZWV0aW5nOiA0OiBQTSwgRGV2LCBRQSwgRGVzaWduZXIsIExlYWQsIFN0YWtlaG9sZGVyXG4gICAgc2VjdGlvbiBFeGVjdXRpb25cbiAgICAgICAgRGV2ZWxvcG1lbnQ6IDM6IERldiwgTGVhZFxuICAgICAgICBUZXN0aW5nOiA0OiBRQSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![ManyActors](../Tests/UserJourney/UserJourneyTests.ManyActors.verified.png)

## Complex

**Input:**
```
journey
    title Complete E-commerce Experience
    
    section Discovery
        Search for product: 4: Customer
        Browse categories: 3: Customer
        Read reviews: 5: Customer
        Compare prices: 4: Customer
    
    section Shopping
        Add to wishlist: 5: Customer
        Add to cart: 4: Customer
        Apply coupon: 2: Customer, Support
        Update quantity: 3: Customer
    
    section Checkout
        Enter shipping: 3: Customer
        Select payment: 4: Customer
        Confirm order: 5: Customer
        Receive confirmation: 5: Customer, System
    
    section Fulfillment
        Order processing: 4: Warehouse, System
        Package shipping: 4: Warehouse, Courier
        Track delivery: 5: Customer, Courier
        Receive package: 5: Customer, Courier
    
    section Post-Purchase
        Leave review: 4: Customer
        Contact support: 2: Customer, Support
        Request return: 1: Customer, Support
        Receive refund: 3: Customer, Finance
```
**Rendered by Mermaid:**
```mermaid
journey
    title Complete E-commerce Experience
    
    section Discovery
        Search for product: 4: Customer
        Browse categories: 3: Customer
        Read reviews: 5: Customer
        Compare prices: 4: Customer
    
    section Shopping
        Add to wishlist: 5: Customer
        Add to cart: 4: Customer
        Apply coupon: 2: Customer, Support
        Update quantity: 3: Customer
    
    section Checkout
        Enter shipping: 3: Customer
        Select payment: 4: Customer
        Confirm order: 5: Customer
        Receive confirmation: 5: Customer, System
    
    section Fulfillment
        Order processing: 4: Warehouse, System
        Package shipping: 4: Warehouse, Courier
        Track delivery: 5: Customer, Courier
        Receive package: 5: Customer, Courier
    
    section Post-Purchase
        Leave review: 4: Customer
        Contact support: 2: Customer, Support
        Request return: 1: Customer, Support
        Receive refund: 3: Customer, Finance
```

[Open in Mermaid Live](https://mermaid.live/edit#base64:eyJjb2RlIjoiam91cm5leVxuICAgIHRpdGxlIENvbXBsZXRlIEUtY29tbWVyY2UgRXhwZXJpZW5jZVxuICAgIFxuICAgIHNlY3Rpb24gRGlzY292ZXJ5XG4gICAgICAgIFNlYXJjaCBmb3IgcHJvZHVjdDogNDogQ3VzdG9tZXJcbiAgICAgICAgQnJvd3NlIGNhdGVnb3JpZXM6IDM6IEN1c3RvbWVyXG4gICAgICAgIFJlYWQgcmV2aWV3czogNTogQ3VzdG9tZXJcbiAgICAgICAgQ29tcGFyZSBwcmljZXM6IDQ6IEN1c3RvbWVyXG4gICAgXG4gICAgc2VjdGlvbiBTaG9wcGluZ1xuICAgICAgICBBZGQgdG8gd2lzaGxpc3Q6IDU6IEN1c3RvbWVyXG4gICAgICAgIEFkZCB0byBjYXJ0OiA0OiBDdXN0b21lclxuICAgICAgICBBcHBseSBjb3Vwb246IDI6IEN1c3RvbWVyLCBTdXBwb3J0XG4gICAgICAgIFVwZGF0ZSBxdWFudGl0eTogMzogQ3VzdG9tZXJcbiAgICBcbiAgICBzZWN0aW9uIENoZWNrb3V0XG4gICAgICAgIEVudGVyIHNoaXBwaW5nOiAzOiBDdXN0b21lclxuICAgICAgICBTZWxlY3QgcGF5bWVudDogNDogQ3VzdG9tZXJcbiAgICAgICAgQ29uZmlybSBvcmRlcjogNTogQ3VzdG9tZXJcbiAgICAgICAgUmVjZWl2ZSBjb25maXJtYXRpb246IDU6IEN1c3RvbWVyLCBTeXN0ZW1cbiAgICBcbiAgICBzZWN0aW9uIEZ1bGZpbGxtZW50XG4gICAgICAgIE9yZGVyIHByb2Nlc3Npbmc6IDQ6IFdhcmVob3VzZSwgU3lzdGVtXG4gICAgICAgIFBhY2thZ2Ugc2hpcHBpbmc6IDQ6IFdhcmVob3VzZSwgQ291cmllclxuICAgICAgICBUcmFjayBkZWxpdmVyeTogNTogQ3VzdG9tZXIsIENvdXJpZXJcbiAgICAgICAgUmVjZWl2ZSBwYWNrYWdlOiA1OiBDdXN0b21lciwgQ291cmllclxuICAgIFxuICAgIHNlY3Rpb24gUG9zdC1QdXJjaGFzZVxuICAgICAgICBMZWF2ZSByZXZpZXc6IDQ6IEN1c3RvbWVyXG4gICAgICAgIENvbnRhY3Qgc3VwcG9ydDogMjogQ3VzdG9tZXIsIFN1cHBvcnRcbiAgICAgICAgUmVxdWVzdCByZXR1cm46IDE6IEN1c3RvbWVyLCBTdXBwb3J0XG4gICAgICAgIFJlY2VpdmUgcmVmdW5kOiAzOiBDdXN0b21lciwgRmluYW5jZSIsIm1lcm1haWQiOnsidGhlbWUiOiJkZWZhdWx0In19)

**Rendered by Naiad:**

![Complex](../Tests/UserJourney/UserJourneyTests.Complex.verified.png)

