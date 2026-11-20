# Product & Software Requirements Specification (SRS): Vivid Imagination

**Project Name:** Vivid Imagination  
**Presentation Title:** *From MVP to Scale: A Modular Monolith Approach to .NET Development*  
**Core Stack:** .NET 10, ASP.NET Core Minimal APIs, PostgreSQL (EF Core), MongoDB, Redis, MassTransit + RabbitMQ, AWS S3 & Amazon Rekognition  

---

## 1. Executive Summary & Product Vision

**Vivid Imagination** is a cross-platform visual discovery and photo-sharing community application. Users publish photos, engage through dynamic voting, curate personalized streams, and rely on a hybrid **Community + AI Content Moderation (Amazon Rekognition)** system that keeps the platform safe while preserving user agency through intelligent UI content blurring.

Architecturally, **Vivid Imagination** serves as a reference **.NET Modular Monolith**. It demonstrates how a team can ship an MVP in a single deployable unit, enforce strict module boundaries (`NetArchTest`), adopt **polyglot persistence** (PostgreSQL for ACID transactional modules like Identity and Social, MongoDB for media metadata, schema-flexible AI moderation reviews, and denormalized feed read-models), isolate integration contracts into **decentralized `*.Contracts` assemblies**, automate **cross-cutting pipeline caching with MediatR and `IStaticCacheManager`**, and scale asynchronously via **MassTransit and RabbitMQ**.

---

## 2. User Personas & System Actors

| Actor | Description |
| :--- | :--- |
| **Anonymous Visitor** | Views the public ranked feed (with sensitive content blurred by default). Cannot upload, vote, or report. |
| **Authenticated Creator / Member** | Signs in via social OAuth/OIDC. Uploads photos, edits/deletes their uploads, upvotes/downvotes, reports content, and toggles sensitive-content blur shields. |
| **Amazon Rekognition (AI Actor)** | Automated cloud vision inspector invoked via `DetectModerationLabelsAsync` to classify uploaded images across nudity, suggestive content, violence, and hate symbols. |
| **Trust & Safety Moderator** | Administrative reviewer who inspects borderline content escalated by the AI engine or community reports. |

---

## 3. Functional Requirements (FR)

### 3.1 Identity & User Management (`IdentityModule` — PostgreSQL)
*   **FR-ID-01 (Social Sign-In):** Users shall authenticate using external OAuth2 / OpenID Connect providers (Google, Facebook, Apple).
*   **FR-ID-02 (Session & Token Issuance):** Upon authentication, the system shall issue stateless JWT access tokens and refresh tokens stored in the `identity` PostgreSQL schema.
*   **FR-ID-03 (Content Safety Preferences):** Authenticated users shall configure a profile preference (`SafeBrowsingMode`: `BlurSensitive` [Default], `HideSensitive`, or `AlwaysShowSensitive`).
*   **FR-ID-04 (Current User Profile):** Exposes `GET /api/v1/identity/users/me` endpoint to fetch the current authenticated profile and preferences.
*   **FR-ID-05 (Startup Role Seeding & Safety Validation):** The system shall seed default roles (`User`, `Admin`, `Moderator`) via `IdentityContextSeed` upon startup using distributed 64-bit Snowflake IDs (`SnowFlakIdGenerator.NewId()`) and uppercase normalized names. Handlers creating users (`RegisterUserCommandHandler`) or provisioning external OAuth accounts (`GoogleSignInCommandHandler`) must verify that required roles exist prior to user creation, failing fast with a `SystemErrorException("ROLE_NOT_CONFIGURED")` if the identity store is uninitialized.

### 3.2 Media Ingestion & Processing (`MediaModule` — MongoDB + AWS S3)
*   **FR-MED-01 (Photo Upload & Metadata):** Authenticated users shall upload photos (JPEG, PNG, WebP) up to 15 MB with a caption (max 500 characters) and optional tags. Metadata is persisted in the `FileEntries` MongoDB collection (`MediaDbContext`).
*   **FR-MED-02 (Direct-to-S3 Presigned Upload):** At scale, the API shall issue presigned AWS S3 URLs so binary payloads upload directly to S3 without tying up web server memory.
*   **FR-MED-03 (Asynchronous Image Optimization):** Upon upload completion, a background worker (`PhotoOptimizationConsumer`) shall generate responsive variants (`Thumbnail: 256px`, `Medium: 1080px`, `Original`) in **WebP** format and compute a compact, pixel-accurate **4x3 DCT BlurHash** placeholder string, publishing `PhotoOptimizedIntegrationEvent`.
*   **FR-MED-04 (Media Lifecycle Management):** Creators shall edit captions or delete their own photos. Deleting performs cascading cleanup pruning the raw file and all responsive WebP variants from S3/Local storage, updates MongoDB, and publishes `PhotoDeletedIntegrationEvent` over MassTransit to trigger cascading cleanup across Feed and Redis.

### 3.3 Autonomous AI & Community Moderation (`ModerationModule` — MongoDB + Amazon Rekognition)
*   **FR-MOD-01 (Event-Driven Zero-Copy AI Inspection):** Newly uploaded photos emit `PhotoUploadedIntegrationEvent(PhotoId, StorageKey)` via MassTransit/RabbitMQ. The `ModerationModule` consumer passes the S3 bucket and `StorageKey` directly to **Amazon Rekognition** (`DetectModerationLabelsAsync`) without downloading image bytes into memory.
*   **FR-MOD-02 (Rekognition Taxonomy & Risk Scoring):** The engine maps Amazon Rekognition's hierarchical `ModerationLabel` taxonomy (`Explicit Nudity`, `Suggestive`, `Violence`, `Visually Disturbing`, `Hate Symbols`) and confidence scores ($0\text{--}100\% \rightarrow 0.00\text{--}1.00$) into three risk dimensions: **Adult**, **Violence**, and **Hate**.
*   **FR-MOD-03 (Three-Tier Automated Policy Enforcement):**
    1.  **Safe ($\text{MaxScore} < 0.50$):** Verdict = `Approved`. Published to feed with normal visibility (`IsUnsafe = false`, `BlurRequired = false`).
    2.  **Sensitive / Flagged ($0.50 \le \text{MaxScore} < 0.85$ or Rekognition `"Suggestive"` taxonomy):** Verdict = `TaggedUnsafe`. Allowed on the feed, but tagged with `IsUnsafe = true`, `BlurRequired = true`, and a specific `SafetyCategory` (e.g., `"Adult Content"`, `"Suggestive"`, `"Graphic Violence"`).
    3.  **Severe Violation ($\text{MaxScore} \ge 0.85$ in Explicit Nudity / Extreme Violence):** Verdict = `Rejected`. Immediately quarantined, purged from public feed projections, and logged for compliance.
*   **FR-MOD-04 (MongoDB Audit Trail):** The `ModerationReview` document—including the raw hierarchical array of Amazon Rekognition `ModerationLabel` objects—shall be persisted in the MongoDB `moderation_reviews` collection.
*   **FR-MOD-05 (UI Blur Shield & Reveal Consent):** For any feed item where `BlurRequired = true`, client applications must render a blurred overlay (`BlurHash` canvas or CSS `backdrop-filter: blur(24px)`) displaying the `SensitiveContentWarning` and an explicit **"Show Content"** toggle button.
*   **FR-MOD-06 (Community Flagging - The 2-Flag Rule):** Authenticated users can report a photo. If a photo receives **2 distinct user reports**, it automatically transitions to `TaggedUnsafe` (`BlurRequired = true`) and is queued for moderator review.

### 3.4 Social Engagement (`SocialModule` — PostgreSQL)
*   **FR-SOC-01 (Voting):** Authenticated users can cast an `Upvote (+1)` or `Downvote (-1)` on any active photo.
*   **FR-SOC-02 (ACID Vote Integrity):** Stored in the `social` PostgreSQL schema with a unique composite constraint `(PhotoId, UserId)` guaranteeing a user can only hold one active vote per photo.
*   **FR-SOC-03 (Downvote Personalization):** Downvoting a photo automatically hides it from that user's personal feed view.

### 3.5 Feed Aggregation & Delivery (`FeedModule` — MongoDB + Redis)
*   **FR-FEED-01 (Denormalized Document Read-Model):** The `FeedModule` subscribes to MassTransit integration events (`PhotoUploaded`, `PhotoModerated`, `PhotoVoted`, `PhotoDeleted`) and maintains pre-joined `FeedItem` documents in the MongoDB `feed_items` collection.
*   **FR-FEED-02 (Global Ranked Stream & Minimal API):** Serves `GET /api/v1/feed` via `FeedApis` dispatching `GetFeedQuery` with pagination. Paged items are ordered by dynamic ranking score:
    $$\text{Score} = \frac{\text{Upvotes} - \text{Downvotes}}{(\text{AgeInHours} + 2)^{1.5}}$$
*   **FR-FEED-03 (Automatic Pipeline Caching):** `GetFeedQuery` implements `ICacheRequest`. Responses are automatically cached in Redis via MediatR's `CachingBehavior` and `IStaticCacheManager`.
*   **FR-FEED-04 (Prefix-Based Cache Invalidation):** All consumers emit `InvalidateFeedCacheCommand` (implementing `IInvalidateCacheRequest` with prefix `vivid:feed`), automatically clearing all cached feed pages on uploads, votes, safety updates, and deletions.

### 3.6 Cross-Platform Mobile Client (`VividImagination.Mobile` — .NET MAUI 10)
*   **FR-MOB-01 (Cross-Platform Photo Ingestion):** The mobile application shall provide native camera and photo library selection (`PhotoUploadPage`), streaming multipart payloads directly to `POST /api/v1/media` with caption and tag metadata.
*   **FR-MOB-02 (Interactive Sensitive Content Shield):** Client feeds consuming `feed_items` where `BlurRequired = true` shall render a dedicated `BlurShieldView` control with blurred overlay, warning badge (`SensitiveContentWarning`), and an interactive reveal-consent toggle button (`Show Content`).
*   **FR-MOB-03 (Dynamic Feed Stream):** The client shall support paginated feed browsing, score-based ordering, and pull-to-refresh connected to `GET /api/v1/feed`.

---

## 4. Polyglot Persistence & Data Architecture

A core teaching point of this Modular Monolith is that **module autonomy enables polyglot persistence**:

| Module | Database Engine | Schema / Collection | Architectural Rationale |
| :--- | :--- | :--- | :--- |
| **`IdentityModule`** | **PostgreSQL** (`EF Core`) | Schema: `identity` | Relational integrity for user accounts, OAuth links, role assignments, and token claims. |
| **`MediaModule`** | **MongoDB** (`MongoDB.Driver`) + **AWS S3** | Collection: `FileEntries` | Document-oriented media asset metadata with tailored sparse and compound indexes + S3 object storage for raw and WebP variants. |
| **`SocialModule`** | **PostgreSQL** (`EF Core`) | Schema: `social` | High-concurrency ACID transactions enforcing unique `(UserId, PhotoId)` vote constraints. |
| **`ModerationModule`** | **MongoDB** (`MongoDB.Driver`) | Collection: `moderation_reviews` | Schema-flexible storage for variable-length Amazon Rekognition `ModerationLabel` hierarchies and AI audit payloads. |
| **`FeedModule`** | **MongoDB** + **Redis** | Collection: `feed_items` | Denormalized, read-optimized documents avoiding cross-module SQL joins; backed by Redis sub-millisecond pipeline caching. |

### 4.1 Document Index Optimization Strategy
MongoDB collections are indexed according to query access patterns to prevent costly collection scans:
*   **`FileEntries` (`MediaDbContext`)**:
    *   `{ Deleted: 1, UploadedTime: -1 }`: Compound index supporting filtered, reverse-chronological pagination of active uploads.
    *   `{ FileLocation: 1 }` (`Unique = true`, `Sparse = true`): Unique sparse constraint guaranteeing file storage key uniqueness while safely allowing unassigned or in-transit documents.
*   **`feed_items` (`FeedDbContext`)**:
    *   `{ Score: -1, UploadedAt: -1 }`: Compound descending index allowing zero-memory sorted feed scans on database fallback.
*   **`moderation_reviews` (`ModerationDbContext`)**:
    *   `{ PhotoId: 1 }`: Primary lookup index for review status and audit trails.
    *   `{ Status: 1, CreatedAt: -1 }`: Compound index for moderator escalation queues.

---

## 5. Messaging & Integration Matrix (MassTransit + RabbitMQ)

All cross-module communication is asynchronous via **MassTransit**. During MVP/Testing, MassTransit uses its `InMemory` transport; at scale, it switches via configuration to **RabbitMQ** exchanges and queues.

Modules declare their consumers autonomously via `IMessagingModule.RegisterConsumers`.

| Integration Event | Source Contracts Assembly | Publisher | Consumer Module(s) | Broker Action |
| :--- | :--- | :--- | :--- | :--- |
| `PhotoUploadedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `MediaModule`, `ModerationModule`, `FeedModule` | `Media` runs background WebP optimization; `Moderation` calls Amazon Rekognition; `Feed` inserts initial document (`Id = PhotoId`) into MongoDB and clears cache. |
| `PhotoOptimizedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `FeedModule` | `Feed` updates `BlurHash` with real pixel hash, sets `StorageKey` to medium WebP variant, and clears cache prefix. |
| `PhotoDeletedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `FeedModule` | `Feed` deletes document from MongoDB and clears cache prefix. |
| `PhotoModeratedIntegrationEvent` | `VividImagination.Modules.Moderation.Contracts` | `ModerationModule` | `FeedModule` | `Feed` updates `IsUnsafe` / `BlurRequired` flags (or purges if `Rejected`) and clears cache prefix. |
| `PhotoVotedIntegrationEvent` | `VividImagination.Modules.Social.Contracts` | `SocialModule` | `FeedModule` | `Feed` updates upvote/downvote counters, recalculates score, and clears cache prefix. |
| `PhotoReportedIntegrationEvent` | `VividImagination.Modules.Social.Contracts` | `SocialModule` | `ModerationModule` | At $\ge 2$ reports, `Moderation` marks item `TaggedUnsafe` and emits `PhotoModeratedIntegrationEvent`. |

---

## 6. Non-Functional Requirements (NFR)

*   **NFR-01 (Feed Read Latency):** $< 25\text{ ms}$ (P95) from Redis cache; $< 80\text{ ms}$ (P95) on MongoDB cache miss.
*   **NFR-02 (Moderation Turnaround):** $< 1.2\text{ seconds}$ (P95) for Amazon Rekognition S3 inspection and RabbitMQ event propagation.
*   **NFR-03 (Automated Test Coverage & Boundary Enforcement):**
    - **104 Passed Unit Tests** covering feature slice commands/queries, domain rules, MediatR pipeline behaviors, WebP transcoding, BlurHash generation, role seeding, and error handling.
    - **7 NetArchTest Boundary Enforcement Tests** executed on every build:
        - Zero direct references between module implementation assemblies.
        - Public integration events restricted exclusively to `*.Contracts` assemblies.
        - Entities and DB contexts strictly encapsulated within their own module namespaces.
*   **NFR-04 (Fail-Safe Moderation Resilience):** If Amazon Rekognition throttles or times out after MassTransit retry policies (exponential backoff), the photo defaults to `TaggedUnsafe` (`BlurRequired = true`) until background re-inspection succeeds.
