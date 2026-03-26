# The Dog API - RestAssured Test Suite

Comprehensive API test suite for [The Dog API](https://thedogapi.com) built with **Java 17**, **JUnit 5**, and **RestAssured**.

## API Coverage

| # | Endpoint | Method | Test Class |
|---|----------|--------|------------|
| 1 | `/breeds` | GET | T01_BreedsTest |
| 2 | `/breeds/:id` | GET | T01_BreedsTest |
| 3 | `/breeds/search` | GET | T01_BreedsTest |
| 4 | `/images/search` | GET | T02_ImageSearchTest |
| 5 | `/images/:image_id` | GET | T02_ImageSearchTest |
| 6 | `/images/upload` | POST | T03_ImageUploadTest |
| 7 | `/images` | GET | T03_ImageUploadTest |
| 8 | `/images/:image_id` | DELETE | T03_ImageUploadTest |
| 9 | `/images/:image_id/breeds` | POST | T04_ImageBreedsTest |
| 10 | `/images/:image_id/breeds` | DELETE | T04_ImageBreedsTest |
| 11 | `/favourites` | POST | T05_FavouritesTest |
| 12 | `/favourites` | GET | T05_FavouritesTest |
| 13 | `/favourites/:id` | GET | T05_FavouritesTest |
| 14 | `/favourites/:id` | DELETE | T05_FavouritesTest |
| 15 | `/votes` | POST | T06_VotesTest |
| 16 | `/votes` | GET | T06_VotesTest |
| 17 | `/votes/:id` | GET | T06_VotesTest |
| 18 | `/votes/:id` | DELETE | T06_VotesTest |

## E2E Flows (T07_E2EFlowsTest)

| # | Flow | Description |
|---|------|-------------|
| 1 | Breed Exploration | List breeds -> Get details -> Search images by breed -> Get image |
| 2 | Multi-Filter Search | Size filter -> Order ASC -> Pagination -> Get image details |
| 3 | Favourite Lifecycle | Search -> Create favourite -> List -> Get -> Delete -> Verify gone |
| 4 | Vote Lifecycle | Search -> Upvote -> Downvote -> List -> Get -> Delete all |
| 5 | Upload Full Journey | Upload -> Tag breed -> Favourite -> Vote -> Full cleanup |
| 6 | Multi-Image Interaction | Favourite 2 images -> Vote differently -> Verify all -> Cleanup |
| 7 | Breed-Filtered Favourite | Get breed -> Search by breed -> Favourite -> Verify -> Cleanup |
| 8 | Upload List Delete | Upload -> List uploaded -> Get details -> Delete -> Verify gone |

## Prerequisites

- Java 17+
- Maven 3.6+
- A free API key from [https://thedogapi.com](https://thedogapi.com)

## Setup

```bash
# Set your API key as an environment variable
export DOG_API_KEY=your-api-key-here
```

## Running Tests

```bash
# Run all tests
mvn test

# Run only smoke tests (basic image search, no API key needed)
mvn test -Psmoke

# Run only E2E flow tests
mvn test -Pe2e

# Run tests by tag
mvn test -Dtest.groups=breeds
mvn test -Dtest.groups=favourites
mvn test -Dtest.groups=votes

# Run a specific test class
mvn test -Dtest=T01_BreedsTest
mvn test -Dtest=T07_E2EFlowsTest
```

## Project Structure

```
dog-api-tests/
├── pom.xml
├── README.md
└── src/test/
    ├── java/com/thedogapi/
    │   ├── config/
    │   │   ├── ApiConfig.java      # Base URL, API key, request specs
    │   │   └── BaseTest.java       # Common setup, requireApiKey()
    │   ├── tests/
    │   │   ├── T01_BreedsTest.java
    │   │   ├── T02_ImageSearchTest.java
    │   │   ├── T03_ImageUploadTest.java
    │   │   ├── T04_ImageBreedsTest.java
    │   │   ├── T05_FavouritesTest.java
    │   │   └── T06_VotesTest.java
    │   └── e2e/
    │       └── T07_E2EFlowsTest.java
    └── resources/
        └── config.properties
```

## Test Tags

| Tag | Description |
|-----|-------------|
| `smoke` | Basic tests that can run without API key |
| `e2e` | End-to-end flow tests |
| `breeds` | Breed endpoint tests |
| `images` | Image search/get tests |
| `upload` | Image upload/delete tests |
| `imageBreeds` | Image breed tagging tests |
| `favourites` | Favourites CRUD tests |
| `votes` | Votes CRUD tests |
| `negative` | Negative/error case tests |

## Notes

- Tests that require an API key will be **skipped** (not fail) if `DOG_API_KEY` is not set.
- All tests hit the live Dog API - no mocking. Tests are subject to rate limits.
- Upload tests create minimal 1x1 PNG files in memory for testing.
- E2E flows include proper cleanup in `finally` blocks to avoid test data leakage.
