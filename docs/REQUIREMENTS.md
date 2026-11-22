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
| **Authenticated Creator / Member** | Signs in via standard credentials (email/username and password) exclusively through OpenIddict OAuth2 `/connect/token`. Uploads photos, edits/deletes their uploads, upvotes/downvotes, reports content, and toggles sensitive-content blur shields. |
| **Amazon Rekognition (AI Actor)** | Automated cloud vision inspector invoked via `DetectModerationLabelsAsync` to classify uploaded images across nudity, suggestive content, violence, and hate symbols. |
| **Trust & Safety Moderator** | Administrative reviewer who inspects borderline content escalated by the AI engine or community reports. |

---

## 3. Functional Requirements (FR)

### 3.1 Identity & User Management (`IdentityModule` — PostgreSQL)
*   **FR-ID-01 (Standard Credential Authentication via OpenIddict OAuth2):** Users shall authenticate exclusively using standard email/username and password credentials via the OpenIddict OAuth2 token endpoint (`POST /connect/token`) supporting Password Grant (`grant_type=password`) and Refresh Token Grant (`grant_type=refresh_token`). External social OAuth/OIDC sign-in providers (Google, Facebook, Apple) and redundant bespoke JSON login endpoints are omitted to maintain strict OAuth2 compliance, centralized lockout protection, and attack surface minimization.
*   **FR-ID-02 (Session & Token Issuance):** Upon successful authentication via `POST /connect/token`, the OpenIddict server engine shall issue cryptographically signed JWT access tokens and sliding refresh tokens stored in the `identity` PostgreSQL schema. Token validation and claims extraction are governed by `OpenIddictValidationAspNetCoreDefaults`.
*   **FR-ID-03 (Content Safety & Personalisation Preferences):** Authenticated users shall configure profile preferences via unified endpoints `GET /api/v1/identity/users/me/preferences` and `PUT /api/v1/identity/users/me/preferences`, controlling `SafeBrowsingMode` (`BlurSensitive` [Default], `HideSensitive`, or `AlwaysShowSensitive`) and `IsPersonalisationEnabled` (Boolean, default `true`).
*   **FR-ID-04 (Current User Profile):** Exposes `GET /api/v1/identity/users/me` endpoint to fetch the current authenticated profile and preferences.
*   **FR-ID-05 (Startup Role Seeding & Safety Validation):** The system shall seed default roles (`User`, `Admin`, `Moderator`) via `IdentityContextSeed` upon startup using distributed 64-bit Snowflake IDs (`SnowFlakIdGenerator.NewId()`) and uppercase normalized names. Handlers creating users (`RegisterUserCommandHandler`) must verify that required roles exist prior to user creation, failing fast with a `SystemErrorException("ROLE_NOT_CONFIGURED")` if the identity store is uninitialized.

### 3.2 Media Ingestion & Processing (`MediaModule` — MongoDB + AWS S3)
*   **FR-MED-01 (Photo Upload, Metadata & User-Authored Tags):** Authenticated creators shall upload photos (JPEG, PNG, WebP) up to 15 MB with a title, caption (max 500 characters), and tags. Creators provide tags either directly via a dedicated UI `Tags` field (`e.g. #nature #sunset #serenity`) or embedded as `#hashtags` in the caption. The client combines these inputs into the photo description/caption, ensuring hashtags are preserved across MongoDB `FileEntries` and denormalized `feed_items` without requiring breaking collection schema migrations.
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

### 3.6 Personalised Feed (`FeedModule` & `IdentityModule`)
*   **FR-PF-001 (Personalised Feed Generation):** The system shall generate personalised photo feeds for authenticated users adapting content ranking based on:
    *   User interests and preferred photo tags (implicitly learned from interaction telemetry or explicitly seeded).
    *   Historical upvotes and downvotes.
    *   Creator engagement and affinity.
    *   Photo popularity and quality.
    *   Content freshness.
    *   Negative interactions and preferences.
*   **FR-PF-002 (Interaction Tracking & Tag Attribution):** The system shall capture user interactions (photo impressions/views, dwell duration, upvotes/downvotes, hides/reports, shares). Client UI telemetry interactions (impressions, dwell duration $\ge 3\text{s}$, shares, in-feed photo hides) shall be ingested via `POST /api/v1/feed/interactions` returning `202 Accepted` and published through MassTransit integration events (`FeedInteractionOccurredIntegrationEvent`), while social actions (upvotes/downvotes) flow via `PhotoVotedIntegrationEvent`. In the background, `FeedInteractionConsumer` extracts the photo's hashtags using `TagExtractor` (normalizing `#Nature` $\rightarrow$ `"nature"`) and attributes affinity adjustments to the user's `UserInterestProfile` (Upvote: `+0.5` per tag, Share: `+0.8` per tag, Dwell $\ge 3\text{s}$: `+0.2` per tag, Downvote: `-0.5` per tag and added to negative preferences). All interactions are processed asynchronously by the `FeedModule` with idempotency guarantees.
*   **FR-PF-003 (User Interest Profiles):** The system shall maintain derived user interest profiles in MongoDB (`user_interest_profiles`) containing normalized tag affinity scores (`TagAffinities: Dictionary<string, double>`), creator affinity scores (`CreatorAffinities: Dictionary<string, double>`), negative tag preferences, negative creator preferences, downvoted/hidden photo exclusions, recently viewed histories, and last updated timestamps. Profiles shall be cached in Redis (`vivid:feed:profile:{userId}`).
*   **FR-PF-004 (Bounded Candidate Generation):** The system shall retrieve a bounded collection of 200–300 candidate photos per request from recent uploads, trending photos, photos matching user interests and preferred tags, photos from high-affinity creators, and low-exposure photos for exploration. Candidates shall be merged and deduplicated prior to ranking.
*   **FR-PF-005 (Normalized Personalised Ranking):** The system shall rank candidate photos using the configurable scoring algorithm:
    $$S = 0.25R + 0.20Q + 0.30T + 0.15C - 0.15N + 0.10E$$
    Where $R$ is Recency, $Q$ is Quality, $T$ is Tag Affinity (evaluating candidate tags against the viewer's normalized `TagAffinities` map in $[0, 1]$), $C$ is Creator Affinity, $N$ is Negative Affinity penalty, and $E$ is Exploration boost. All ranking signals shall be normalized before calculating the composite score.
*   **FR-PF-006 (Content Filtering & Diversity):** The personalised feed shall:
    *   Exclude photos previously downvoted or hidden by the user.
    *   Exclude rejected or quarantined content.
    *   Respect the user's `SafeBrowsingMode`.
    *   Limit consecutive photos from the same creator to at most 2.
    *   Encourage diversity across tags and content categories.
    *   Avoid repeatedly displaying recently viewed photos.
    *   Reserve 10–20% of feed recommendations for exploration.
*   **FR-PF-007 (Cold-Start Handling):** Users without sufficient interaction history (< 3 interactions) shall receive the existing global ranked feed fallback until adequate data is collected.
*   **FR-PF-008 (Pagination & Caching):** The personalised feed shall support cursor-based and page-based pagination. Redis shall cache user interest profiles and personalised feed slices to optimize retrieval latency.
*   **FR-PF-009 (Personalisation Preferences):** Authenticated users shall be able to enable or disable personalised recommendations via the unified `/api/v1/identity/users/me/preferences` profile preferences endpoint (`IsPersonalisationEnabled`). When disabled, the system shall serve the global ranked feed.

### 3.7 Cross-Platform Mobile Client (`VividImagination.Mobile` — .NET MAUI 10)
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
| **`FeedModule`** | **MongoDB** + **Redis** | Collections: `feed_items`, `user_interest_profiles`, `user_interactions` | Denormalized, read-optimized documents avoiding cross-module SQL joins; backed by Redis sub-millisecond pipeline caching. |

### 4.1 Document Index Optimization Strategy
MongoDB collections are indexed according to query access patterns to prevent costly collection scans:
*   **`FileEntries` (`MediaDbContext`)**:
    *   `{ Deleted: 1, UploadedTime: -1 }`: Compound index supporting filtered, reverse-chronological pagination of active uploads.
    *   `{ FileLocation: 1 }` (`Unique = true`, `Sparse = true`): Unique sparse constraint guaranteeing file storage key uniqueness while safely allowing unassigned or in-transit documents.
*   **`feed_items` (`FeedDbContext`)**:
    *   `{ Score: -1, UploadedAt: -1 }`: Compound descending index allowing zero-memory sorted feed scans on database fallback.
*   **`user_interest_profiles` (`FeedDbContext`)**:
    *   `{ UserId: 1 }` (`Unique = true`): High-throughput primary index for user affinity lookups and caching.
*   **`user_interactions` (`FeedDbContext`)**:
    *   `{ EventId: 1 }` (`Unique = true`): Strict idempotency constraint preventing duplicate event processing.
    *   `{ UserId: 1, Timestamp: -1 }`: Compound index for user interaction timeline extraction.
*   **`moderation_reviews` (`ModerationDbContext`)**:
    *   `{ PhotoId: 1 }`: Primary lookup index for review status and audit trails.
    *   `{ Status: 1, CreatedAt: -1 }`: Compound index for moderator escalation queues.

---

## 5. Messaging & Integration Matrix (MassTransit + RabbitMQ)

All cross-module communication is asynchronous via **MassTransit**, with synchronous entity verification supported via MassTransit `IRequestClient<T>`. During MVP/Testing, MassTransit uses its `InMemory` transport; at scale, it switches via configuration to **RabbitMQ** exchanges and queues.

Modules declare their consumers autonomously via `IMessagingModule.RegisterConsumers`.

| Integration Message / Contract | Source Contracts Assembly | Publisher / Caller | Consumer Module(s) | Broker Action |
| :--- | :--- | :--- | :--- | :--- |
| `PhotoUploadedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `MediaModule`, `ModerationModule`, `FeedModule` | `Media` runs background WebP optimization; `Moderation` calls Amazon Rekognition; `Feed` inserts initial document (`Id = PhotoId`) into MongoDB and clears cache. |
| `PhotoOptimizedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `FeedModule` | `Feed` updates `BlurHash` with real pixel hash, sets `StorageKey` to medium WebP variant, and clears cache prefix. |
| `PhotoDeletedIntegrationEvent` | `VividImagination.Modules.Media.Contracts` | `MediaModule` | `FeedModule` | `Feed` deletes document from MongoDB and clears cache prefix. |
| `PhotoModeratedIntegrationEvent` | `VividImagination.Modules.Moderation.Contracts` | `ModerationModule` | `FeedModule` | `Feed` updates `IsUnsafe` / `BlurRequired` flags (or purges if `Rejected`) and clears cache prefix. |
| `PhotoVotedIntegrationEvent` | `VividImagination.Modules.Social.Contracts` | `SocialModule` | `FeedModule` | `Feed` updates upvote/downvote counters, recalculates score, and clears cache prefix. |
| `PhotoReportedIntegrationEvent` | `VividImagination.Modules.Social.Contracts` | `SocialModule` | `ModerationModule` | At $\ge 2$ reports, `Moderation` marks item `TaggedUnsafe` and emits `PhotoModeratedIntegrationEvent`. |
| `FeedInteractionOccurredIntegrationEvent` | `VividImagination.Modules.Feed.Contracts` | `FeedModule` / Clients | `FeedModule` | `Feed` updates `UserInterestProfile` tag and creator affinities, tracks negative exclusions, and evicts user feed caches. |
| `ValidatePhotoRequest` / `PhotoValidationResponse` | `VividImagination.Modules.Media.Contracts` | `SocialModule`, `ModerationModule` | `MediaModule` | Synchronous MassTransit RPC: queries `FileEntries` in MongoDB to verify photo existence without shared database access. |

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
