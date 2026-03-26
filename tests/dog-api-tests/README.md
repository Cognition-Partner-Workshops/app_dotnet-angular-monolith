# The Dog API - API Test Suite

Comprehensive API test suite for [The Dog API](https://thedogapi.com) with end-to-end flow coverage.

## API Coverage

| Section | Endpoints | Tests |
|---------|-----------|-------|
| **Breeds** | `GET /breeds`, `GET /breeds/:id` | Listing, pagination, get by ID, negative cases |
| **Images Search** | `GET /images/search`, `GET /images/:id` | Filters (size, mime_type, has_breeds, order), pagination, get by ID |
| **Images Upload** | `POST /images/upload`, `GET /images/`, `DELETE /images/:id` | Upload, list uploaded, delete, auth checks |
| **Image Breeds** | `GET /images/:id/breeds`, `POST /images/:id/breeds`, `DELETE /images/:id/breeds/:breed_id` | Tag/untag breeds, auth checks |
| **Favourites** | `GET /favourites`, `GET /favourites/:id`, `POST /favourites`, `DELETE /favourites/:id` | Full CRUD lifecycle |
| **Votes** | `GET /votes`, `GET /votes/:id`, `POST /votes`, `DELETE /votes/:id` | Full CRUD lifecycle, upvote/downvote |

## E2E Flows

1. **Breed Exploration -> Image Discovery**: List breeds -> Get breed details -> Search images by breed
2. **Multi-Filter Image Search**: Apply size/mime/breed filters -> Verify results -> Get individual details
3. **Favourite Lifecycle**: Search image -> Favourite -> List -> Get -> Delete -> Verify deletion
4. **Vote Lifecycle**: Search image -> Vote -> List -> Get -> Delete -> Verify deletion
5. **Full Image Journey**: Upload -> Verify -> Tag breed -> Favourite -> Vote -> Full cleanup
6. **Multi-Image Interactions**: Favourite & vote on multiple images -> Verify all -> Cleanup
7. **Breed-Filtered Favourite**: Find breed images -> Favourite one -> Verify -> Cleanup
8. **Upload List Delete**: Upload -> Find in list -> Get details -> Delete -> Verify gone

## Setup

```bash
# Install dependencies
pip install -r requirements.txt

# Copy and configure environment
cp .env.example .env
# Edit .env and add your API key from https://thedogapi.com
```

## Running Tests

```bash
# Run all tests
pytest

# Run only smoke tests (no API key needed)
pytest -m smoke

# Run only breed tests
pytest -m breeds

# Run only E2E flows
pytest -m e2e

# Run excluding tests that need API key
pytest -m "not requires_api_key"

# Generate HTML report
pytest --html=report.html --self-contained-html
```

## Test Markers

| Marker | Description |
|--------|-------------|
| `smoke` | Quick smoke tests for basic connectivity |
| `images` | Tests for /images endpoints |
| `breeds` | Tests for /breeds endpoints |
| `favourites` | Tests for /favourites endpoints |
| `votes` | Tests for /votes endpoints |
| `e2e` | End-to-end flow tests |
| `negative` | Negative / error scenario tests |
| `requires_api_key` | Tests that require a valid API key |
