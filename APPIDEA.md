**Build a Windows .exe app that helps authors research Amazon keywords, categories, competitors, and ads—similar in scope to “Publisher Rocket”**
Target: **C# .NET 8 + WPF** (native feel, fast, easy MSI/EXE packaging). Use **MVVM** pattern. Keep all data local (SQLite). Provide a clean, modern UI.

> ⚠️ Compliance note
>
> * Implement polite scraping with low concurrency, randomized delays, caching, and an explicit “Respect site rules” toggle.
> * Clearly surface a Settings screen where users can set pacing, add their own Amazon cookies (optional), choose markets, and read a disclaimer.
> * Do **not** bypass paywalls or captchas, and show a warning if rate-limited.
> * Ship an internal **robots-style allowlist** and **maximum request budget** per minute.
> * Make all scraping modules **pluggable** so a non-scraping “manual import/CSV” mode also works.

---

Calculator: https://kindlepreneur.com/amazon-kdp-sales-rank-calculator/

## 0) Deliverables (first output)

1. A **solution scaffold** with projects:

   * `KDP-CATEGORY-RANKERApp` (WPF, MVVM)
   * `KDP-CATEGORY-RANKERCore` (domain models, services, algorithms)
   * `KDP-CATEGORY-RANKERData` (SQLite EF Core, migrations, repositories)
   * `KDP-CATEGORY-RANKERScraping` (HTTP client, parsers, market adapters, caching)
   * `KDP-CATEGORY-RANKERTests` (xUnit)
2. A **README** with build/run steps, packaging instructions (single-file EXE via `PublishSingleFile=true`) and how to run an **offline demo** using bundled sample datasets.
3. A **UI style guide** (colors, spacing, typography) and a **UX flow map**.
4. Initial **feature slices** runnable end-to-end with mocked data, then live mode behind a toggle.

---

## 1) High-level features (match the spec below)

* **Keyword Research**

  * Find 7+ profitable keywords quickly. Columns: *Keyword*, *Competitive Score (0–100)*, *Est. Searches/Month*, *Avg Monthly Earnings*, *Trend*, *Markets present*.
  * Competitive Score = blend of title/subtitle usage, top-10 strength (ratings, BSR), and SERP saturation.
  * Autocomplete expansion (seed → variants).
  * Batch mode & CSV export.

* **Category Analyzer (19,000+ categories)**

  * List candidate categories, show: *Sales to #1*, *Sales to #10*, *Avg Price (Indie/Big 5)*, *Avg Rating*, *Avg Page Count*, *Avg Age (days)*, *% Large Publisher*, *% KU*.
  * **Historic Category Feature**: store monthly snapshots (local time-series) and render a trend chart with linear regression and a monthly growth label: *rapidly declining (<−20%)*, *significantly declining (−15%→−5%)*, *flat (−5%→5%)*, *growing (5%→15%)*, *rapidly growing (>20%)*.
  * **Duplicate category detection**: flag duplicates when different breadcrumb strings resolve to the same list page ID (canonicalized URL).
  * **Ghost category detection**: flag when the category page lacks a visible name/breadcrumb or can’t be reached by browsing (hidden endpoint). Warn users those don’t award bestseller tags and mainly “roll up” to parent categories.

* **Competition Analyzer**

  * For any keyword/category: fetch top 30 books. Columns: *Title*, *Author*, *Format*, *Price*, *Pages (or audio minutes)*, *Ratings/Avg*, *KU?*, *Publisher (inferred)*, *BSR*, *Est. Daily Sales*, *Est. Monthly Earnings*.
  * “Check it out” button to open the Amazon page in the system browser.

* **Reverse ASIN**

  * Input a book ASIN → show related terms: title-token N-grams, subtitle N-grams, series tokens, also-bought titles/authors, category terms, review keyphrases, product metadata (dimensions/age range if kids), inferred “seed keywords.”
  * Export list and one-click add to AMS generator.

* **AMS Keyword Generator**

  * Generate hundreds of ad keywords: seed keywords + competitor author names + related category terms + morphological variants + smart N-grams.
  * Allow **ASIN harvesting** for product-targeting lists.
  * De-dup, normalize, optional negative keywords, and CSV export.

* **International markets**

  * Support .com, .co.uk, .de, .fr, .es, .it, .ca, .com.au.
  * Market selector (multi-select), per-market metrics, and a combined view.

* **Tutorials & Help**

  * “Getting Started” tour, inline tooltips, and links to a local help file.
  * Include a “Free Amazon Ads Course” link that opens an external URL (placeholder).

* **Version & Update Log**

  * Local “Updates” screen with a JSON changelog stub. (No cloud needed; just simulate for now.)

---

## 2) Data model & persistence

Use **SQLite** with EF Core. Key tables:

* `Keywords(Id, Text, Market, CreatedAt)`
* `KeywordMetrics(KeywordId, Market, Month, Searches, CompetitiveScore, AvgMonthlyEarnings, SnapshotAt)`
* `Categories(Id, Market, CanonicalId, Breadcrumb, IsGhost, IsDuplicateGroupId)`
* `CategorySnapshot(CategoryId, Month, SalesToNo1, SalesToNo10, AvgPriceIndie, AvgPriceBig, AvgRating, AvgPageCount, AvgAgeDays, KUPct, LargePublisherPct, SnapshotAt)`
* `Books(ASIN, Market, Title, Author, Price, PagesOrMinutes, KUParticipation, PublisherType, RatingAvg, RatingsCount)`
* `BookSnapshot(ASIN, Market, CapturedAt, BSR, EstDailySales, EstMonthlyEarnings, CategoryIdsCsv)`
* `ReverseAsinTerms(ASIN, Market, Term, Source, StrengthScore)`
* `Settings(Key, ValueJson)`

Provide migrations + seed scripts with fake/demo data for offline mode.

---

## 3) Algorithms (concrete, implement now)

### 3.1 ABSR → Estimated Sales

Implement a **pluggable estimator** (strategy pattern) with format-aware curves (Paperback vs Kindle). Start with a **power-law** baseline; expose coefficients in `appsettings.json`.

Example (adjustable):

```csharp
// Kindle default (approx):
sales = a * Math.Pow(absr, b); // a = 5500, b = -0.83
// Print default (approx):
sales = a * Math.Pow(absr, b); // a = 2600, b = -0.75
```

* Clamp at min 0.0.
* Recompute **SalesTo#1** for a category by reading current #1 ABSR and running the estimator. **SalesTo#10** = 10th book’s estimated daily sales.
* Add a “Coefficient Tuner” dev tool (hidden menu) to tweak `a` and `b` and persist them.

### 3.2 Competitive Score (0–100)

Compute:

* **SERP intensity**: median of top-10 ratings count (scaled 0–30) + rating avg (0–10).
* **Keyword on-page usage**: presence in title/subtitle (0–20).
* **Top-10 BSR toughness**: inverse of median BSR (0–25).
* **Saturation**: # of distinct top-100 titles with exact/close match (0–15).
  Normalize to 0–100 and show a 5-band label (*Very Low*→*Very High*).

### 3.3 Searches/Month (proxy)

Blend:

* Autocomplete breadth (A…Z, 0–20 pts → linear map to volume)
* Rank of the keyword on category pages (if present)
* Frequency of term in competitor titles/subtitles (normalized)
  Expose scale factors in config.

### 3.4 Avg Monthly Earnings

`EstDailySales * AvgPrice * 30 * revenueFactor` where `revenueFactor` defaults to 0.6 (after Amazon split/fees) and is configurable.

### 3.5 Ghost/Duplicate detection

* **Duplicate**: normalize category page URL; if multiple breadcrumbs resolve to same canonical segment or `node=` value, assign same `DuplicateGroupId`.
* **Ghost**: page lacks breadcrumb trail or page title; or `node` not reachable from the root category tree crawler. Mark `IsGhost=true`, annotate parent category to which it rolls up.

### 3.6 Historic trend & volatility

* For each `CategorySnapshot.Month`, compute sum of `EstDailySales` across top 30 × 30 days → “Category Monthly Sales Index.”
* Fit simple linear regression over the last 12 months. Growth % = slope / mean \* 100.
* Flag **high variation** if stdev of top-30 sales share of the max book > threshold (e.g., single title >35% of monthly total).

### 3.7 AMS keyword generation

* Sources: seed terms, author names (from competitors), category tokens, review keyphrases (noun-phrases via simple POS regex), pluralization/stemming, phrase permutations (2- to 4-grams), typo variants (Levenshtein distance 1).
* De-dup, strip stopwords, and export CSV with columns: *Keyword*, *MatchType*, *Source*, *Notes*.
* ASIN list for product targeting from top competitors and “also-bought”.

---

## 4) Scraping architecture (pluggable, throttled)

* `IAmazonMarketClient` per market (.com, .co.uk, .de, etc.) implementing:

  * `GetSearchResults(keyword, page)`
  * `GetCategoryPage(categoryId, page)`
  * `GetBookDetail(asin)`
  * `GetAlsoBought(asin)`
  * `GetBreadcrumbAndNode(url)`
* **HttpClient** with:

  * Randomized user agent pool, exponential backoff, jittered delays (user-configurable min/max).
  * Response cache (SQLite) keyed by canonical URL + ETag/Last-Modified.
  * Parse with **AngleSharp** or **HtmlAgilityPack**.
* A **Category Tree Crawler** (per market) that can run overnight (optional) or import from bundled seed JSON.

**Offline demo mode**: provide JSON fixtures so the app runs without network.

---

## 5) UI/UX (WPF, MVVM, modern)

* **Home Dashboard**: 4 big cards (“Keywords,” “Categories,” “Competition,” “AMS”), plus “Recent Projects,” “Last Sync,” and an “Updates” tile.
* **Keyword Research**: left search pane, results grid, filters (competition/volume), bulk actions, chart sparkline for trend. “Add to AMS” & “Analyze Competition” buttons.
* **Category Analyzer**: searchable tree + table; stats header (8 tiles); **Historic graph** (single chart with regression line); badges for *Ghost*/*Duplicate*.
* **Competition Analyzer**: result grid with sortable columns; image thumbnails; “Check it Out” external link; “Export.”
* **Reverse ASIN**: input box (one or many ASINs), show term cloud + table with strength score and source.
* **AMS Generator**: tabbed: *Keywords*, *ASIN Targets*, *Negatives*; controls for permutations, match types, export.
* **International**: market picker at top; “All markets” mode merges stats and shows per-market columns if expanded.
* **Settings**: markets, rate limits, cache size, estimator coefficients, revenue factor, theme (light/dark), privacy/disclaimer.
* **Help/Tutorials**: embedded markdown pages and a simple “tour.”

Use a sleek aesthetic: ample whitespace, rounded corners, subtle shadows, semibold headers, consistent 8-pt spacing grid. Provide a `Theme.xaml`.

---

## 6) Testing

* xUnit tests for: estimators, parsers (HTML fixtures), duplicate/ghost detection, keyword scoring, trend calculations, and CSV exports.
* Snapshot tests on HTML parsers using saved fixtures for each market.
* Integration test that runs in **offline demo mode** to validate the end-to-end flow.

---

## 7) Packaging

* Publish as **single-file, self-contained** win-x64.
* Create an **MSI** with app icon, Start Menu entry, file associations for `.KDP-Category-ranker`.
* Include a **first-run wizard** to choose markets and data directory.
* Optional: simple “license key” gate with a 30-day trial (local only, hashed machine ID).

---

## 8) Acceptance criteria (must pass)

1. I can enter a seed keyword → see a list with Competitive Score, Estimated Searches/Month, Avg Monthly Earnings, Trend, and per-market presence, and export to CSV.
2. I can open a category → see Sales to #1/#10, 8 stat tiles, historic chart, and ghost/duplicate badges.
3. I can input an ASIN → get a table of inferred keywords & related ASINs, then send them to the AMS generator.
4. For any top-30 list, **Est. Daily Sales** is computed via the ABSR estimator and is configurable.
5. All features work in **offline demo** with bundled fixtures.
6. App builds to a **single EXE/MSI** with clear README instructions.

---

## 9) Libraries & NuGet

* **UI**: MahApps.Metro or FluentWPF (choose one), LiveCharts2 for charts.
* **HTTP/Parsing**: AngleSharp or HtmlAgilityPack; Polly for retries/backoff.
* **Data**: EF Core SQLite; CsvHelper.
* **DI/Config**: Microsoft.Extensions.\* packages.
* **Testing**: xUnit, FluentAssertions.
* **Localization**: .resx scaffolding for future i18n.

---

## 10) Seed data (bundle for offline demo)

* 3 markets (.com, .co.uk, .de) with:

  * 50 sample keywords (varied comp/volume).
  * 30 categories (with one duplicate group and two ghosts flagged).
  * 60 books with plausible BSR/ratings/prices/pages.
  * 12 months of category snapshots to render a trend line.

---

## 11) Nice-to-haves (if time permits)

* “Zoom in/out” control on charts.
* “Saved workspaces” (remember last grids/filters).
* Simple update checker that reads a local/remote JSON (no auto-update required).
* Chromebook/ARM build note in README for future work.

---

**Now please:**

1. Generate the full solution (file tree + key code files).
2. Provide the README and UI/UX notes.
3. Implement mocked data first, then wire the live scraper behind a feature flag.
4. Include unit tests and one integration test that runs the offline path.
5. Finish with packaging instructions (`dotnet publish` command and MSI script).