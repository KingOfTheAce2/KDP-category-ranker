# KDP Category Ranker

The ultimate Windows desktop application for finding the **BEST KDP categories** for your book and planning your path to bestseller status. Built with C# .NET 8 and WPF, featuring advanced category recommendation algorithms and bestseller planning tools.

## 🚀 Key Features

### 🎯 **Category Recommender (NEW!)** 
The main feature that sets this tool apart:
- **Smart Category Recommendations**: AI-powered analysis finds the best categories for YOUR specific book
- **Bestseller Planning**: Calculate exactly how many daily sales you need to reach #1, Top 10, or Top 50
- **Difficulty Scoring**: Each category gets a 0-100 difficulty score so you know what you're up against
- **Revenue Projections**: See potential monthly earnings at different bestseller positions
- **Optimal Release Timing**: Find the easiest months to launch in each category
- **Success Probability**: Get realistic odds based on your sales targets vs. category requirements

### 📊 **Advanced Analytics**
- **19,000+ Categories**: Complete Amazon KDP category database with real-time metrics
- **Daily Sales Requirements**: Know exactly how many books to sell for bestseller status
- **Historic Trends**: 12-month category performance with linear regression analysis
- **Ghost Category Detection**: Avoid categories that don't award bestseller tags
- **Seasonal Patterns**: Identify the best times to launch in each category

### 🔍 **Research Tools**
- **Keyword Research**: Find profitable keywords with competitive scoring and search volume estimates
- **Competition Analysis**: Research top competitors and identify market opportunities  
- **Reverse ASIN**: Extract keywords and related terms from competitor books
- **AMS Keyword Generator**: Generate hundreds of Amazon advertising keywords
- **International Markets**: Support for 8 major Amazon markets (.com, .co.uk, .de, .fr, .es, .it, .ca, .com.au)

### Analytics & Intelligence
- **Competitive Scoring**: Advanced algorithm analyzing SERP intensity, keyword usage, BSR toughness, and market saturation
- **Sales Estimation**: Configurable BSR-to-sales conversion with format-specific coefficients
- **Trend Analysis**: Linear regression analysis with growth percentage calculations
- **Ghost Category Detection**: Identify hidden/unreachable categories
- **Duplicate Category Detection**: Flag categories with the same underlying data

### Technical Features  
- **Offline Demo Mode**: Full functionality with bundled sample data
- **Rate-Limited Scraping**: Respectful data collection with configurable throttling
- **SQLite Database**: Local data storage with comprehensive schema
- **CSV Export**: Export data to spreadsheets for further analysis
- **Caching System**: Intelligent caching reduces network requests
- **Modern WPF UI**: Clean, responsive interface with MahApps.Metro

## 🏗️ Architecture

### Project Structure
```
KDP-CATEGORY-RANKER/
├── src/
│   ├── KDP-CATEGORY-RANKERApp/          # WPF Application (MVVM)
│   ├── KDP-CATEGORY-RANKERCore/         # Domain Models & Business Logic
│   ├── KDP-CATEGORY-RANKERData/         # Entity Framework & Data Access
│   ├── KDP-CATEGORY-RANKERScraping/     # HTTP Clients & Parsers
├── tests/
│   └── KDP-CATEGORY-RANKERTests/        # Unit & Integration Tests
├── KDP-CATEGORY-RANKER.sln             # Visual Studio Solution
└── README.md
```

### Technology Stack
- **.NET 8**: Target framework
- **WPF + MVVM**: UI framework with clean architecture
- **Entity Framework Core**: Data access with SQLite
- **MahApps.Metro**: Modern UI components and theming
- **AngleSharp**: HTML parsing for scraping
- **Polly**: Resilience and retry policies
- **xUnit**: Testing framework
- **CommunityToolkit.Mvvm**: MVVM helpers

## 🔧 Prerequisites

- **Windows 10/11** (x64)
- **.NET 8 SDK** (for development) - Download from https://dotnet.microsoft.com/download/dotnet/8.0
- **Visual Studio 2022** (for development, Community edition is free)

## 📦 Installation & Setup

### Option 1: Build and Run from Source
```bash
# Clone or download this repository
# Navigate to the project directory

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application
dotnet run --project src/KDP-CATEGORY-RANKERApp
```

### Option 2: Single-File Deployment (Production)
```bash
# Build single-file executable
dotnet publish src/KDP-CATEGORY-RANKERApp -c Release -r win-x64 \
  --self-contained true /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true

# Find the executable in: src/KDP-CATEGORY-RANKERApp/bin/Release/net8.0-windows/win-x64/publish/
```

## 🚀 Quick Start

### First Run
1. **Launch the application** using `dotnet run --project src/KDP-CATEGORY-RANKERApp`
2. **Click "🚀 Category Recommender"** - The main feature for finding your best categories
3. **Enter your book details** - Title, keywords, price, and your realistic daily sales target
4. **Get recommendations** - See ranked categories with difficulty scores and bestseller requirements

### Category Recommender Workflow
1. **📝 Book Details**: Enter your book title, description, keywords, format, and price
2. **🎯 Set Goals**: Define your realistic daily sales target (be honest!)
3. **🔍 Find Categories**: Get AI-powered recommendations ranked by opportunity score
4. **📈 Plan Success**: See exactly what it takes to hit bestseller in each category
5. **⏰ Time It Right**: Discover the optimal months to launch for easier ranking
6. **📊 Make Decisions**: Choose categories where you have the highest success probability

### Other Tools
- **Keywords** - Research profitable keywords with competition analysis
- **Categories** - Browse and analyze all 19,000+ KDP categories  
- **Competition** - Study top-performing books in any niche
- **AMS Generator** - Create Amazon advertising campaigns

### Demo Mode
The application runs in offline demo mode by default, using sample data to demonstrate all features without requiring network access.

## 📊 Key Features Deep Dive

### Competitive Scoring Algorithm
The competitive score (0-100) is calculated using:
- **SERP Intensity** (30%): Median ratings count + average rating
- **Keyword Usage** (20%): Presence in titles/subtitles  
- **BSR Toughness** (25%): Inverse of median BSR in top 10
- **Market Saturation** (15%): Number of exact matches in top 100
- **Search Volume Factor** (10%): Estimated based on autocomplete breadth

### Sales Estimation
Uses configurable power-law curves:
```csharp
// Kindle: sales = 5500 * BSR^(-0.83)
// Print: sales = 2600 * BSR^(-0.75)
```

Coefficients can be tuned via the settings panel or `appsettings.json`.

### Historic Trend Analysis  
- Linear regression over 12 months of category data
- Growth categories: >5% monthly growth
- Declining categories: <-5% monthly decline  
- High variation detection for volatile markets

## ⚙️ Configuration

### App Settings (`src/KDP-CATEGORY-RANKERApp/appsettings.json`)
```json
{
  "Scraping": {
    "MaxRequestsPerMinute": 10,
    "DelayBetweenRequestsMs": 2000,
    "MaxConcurrency": 2,
    "RespectRobotsTxt": true
  },
  "SalesEstimation": {
    "Kindle": { "CoefficientA": 5500.0, "CoefficientB": -0.83 },
    "Print": { "CoefficientA": 2600.0, "CoefficientB": -0.75 }
  }
}
```

### Rate Limiting & Ethics
- Configurable request limits (default: 10/minute)
- Randomized delays between requests
- User-agent rotation
- Cache-first approach to minimize requests

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Categories
```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only  
dotnet test --filter Category=Integration
```

### Test Coverage
- **Unit Tests**: Core algorithms and business logic
- **Integration Tests**: Database operations and API integrations
- **Mock Data Tests**: Full workflow with sample data

## 🔒 Compliance & Ethics

### Scraping Ethics
- **Rate Limiting**: Configurable, respectful request rates
- **Caching**: Reduces unnecessary requests
- **No Bypass**: Does not circumvent CAPTCHAs or paywalls

### Data Privacy
- **Local Storage**: All data stored locally in SQLite
- **No Cloud**: No data transmitted to external services
- **User Control**: Full control over data collection settings

## 📈 Performance

### System Requirements
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 1GB for application + database growth
- **Network**: Broadband recommended for live scraping
- **CPU**: Any modern 64-bit processor

## 🛠️ Development

### Key Components
- **Services**: Business logic in `KDP-CATEGORY-RANKERCore/Services/`
- **Data Models**: EF Core entities in `KDP-CATEGORY-RANKERData/Models/`
- **UI**: WPF views and view models in `KDP-CATEGORY-RANKERApp/`
- **Scraping**: HTTP clients in `KDP-CATEGORY-RANKERScraping/Services/`

### Architecture Patterns
- **MVVM**: Clean separation of UI and business logic
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic encapsulation
- **Dependency Injection**: Loose coupling and testability

## 📄 License

This project is for educational and demonstration purposes. Always comply with Amazon's Terms of Service and respect rate limits.

## 🆘 Support & Issues

For issues or questions, please check the code comments and configuration files. This is a demonstration project showcasing modern .NET development practices.

---

**⚠️ Important Notice**: This tool demonstrates web scraping techniques for educational purposes. Always comply with website Terms of Service and respect rate limits. The authors are not responsible for any misuse of this software.

**🎯 Perfect for**: Learning modern .NET development, WPF applications, web scraping techniques, and building data-driven desktop applications.
