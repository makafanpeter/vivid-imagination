# Vivid Imagination — .NET Modular Monolith Reference Architecture

**Presentation Topic:** *From MVP to Scale: A Modular Monolith Approach to .NET Development*

**Vivid Imagination** is a visual photo-sharing community application engineered as a **.NET 10 Modular Monolith**. It demonstrates how to evolve from a fast MVP into a scalable, event-driven system with:
- **Autonomous AI Moderation via Amazon Rekognition (`AWSSDK.Rekognition`)**
- **Unsafe Tagging & UI Blur Shield (`BlurRequired = true`, `BlurHash`)**
- **Polyglot Persistence (PostgreSQL + MongoDB)**
- **Asynchronous Messaging (MassTransit + RabbitMQ)**
- **Compile-Time Boundary Enforcement (`NetArchTest`)**

---

## Documentation Suite

- 📋 **[Product & Software Requirements Specification (SRS)](docs/REQUIREMENTS.md)**
- 🏗️ **[Modular Monolith Scope & Architecture Case Study](docs/ARCHITECTURE_SCOPE.md)**
- 🎤 **[Speaker Deck & Presentation Narrative Guide](docs/PRESENTATION_GUIDE.md)**

---

## Architecture & Module Breakdown

| Module | Responsibility | Database Engine | Schema / Collection |
| :--- | :--- | :--- | :--- |
| **`IdentityModule`** | OAuth2/OIDC sign-in, JWT tokens, safe-browsing preferences | **PostgreSQL** (`EF Core`) | `identity.*` |
| **`MediaModule`** | S3 photo uploads, WebP resizing, BlurHash generation | **PostgreSQL** (`EF Core`) + **AWS S3** | `media.*` |
| **`ModerationModule`** | **Amazon Rekognition** safety scan, unsafe tagging, audit trail | **MongoDB** (`MongoDB.Driver`) | `moderation_reviews` |
| **`SocialModule`** | Upvotes/downvotes, 2-flag community reports | **PostgreSQL** (`EF Core`) | `social.*` |
| **`FeedModule`** | Denormalized feed read-models, dynamic ranking, UI blur flags | **MongoDB** + **Redis** | `feed_items` |

---

## Quick Start

### 1. Start Infrastructure (PostgreSQL, MongoDB, RabbitMQ)
```bash
docker compose up -d
```

### 2. Run Architecture & Unit Tests
```bash
dotnet test
```

### 3. Run the Modular Monolith Host API
```bash
dotnet run --project src/backend/Host/VividImagination.Api
```
