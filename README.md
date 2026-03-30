# DevinReactAEMProject

A comprehensive React + Adobe Experience Manager (AEM) project demonstrating custom components, workflows, content fragments, experience fragments, sling jobs, schedulers, event listeners, and full React integration.

## Project Structure

```
DevinReactAEMProject/
├── pom.xml                 # Parent POM (reactor)
├── core/                   # OSGi bundle (Java backend)
│   ├── pom.xml
│   └── src/main/java/com/devin/aem/core/
│       ├── models/         # Sling Models (8+ models)
│       ├── services/       # OSGi Services (3 services)
│       ├── servlets/       # Sling Servlets (3 servlets)
│       ├── schedulers/     # OSGi Schedulers (2 schedulers)
│       ├── listeners/      # Event Listeners (4 listeners)
│       ├── workflows/      # Workflow Process Steps (3 workflows)
│       ├── jobs/           # Sling Jobs (2 consumers + 1 creator)
│       └── filters/        # Sling Filters (1 logging filter)
├── ui.apps/                # AEM components and templates
│   ├── pom.xml
│   └── src/main/content/jcr_root/apps/devinreactaem/
│       ├── components/     # 15+ HTL/Sightly components with dialogs
│       ├── clientlibs/     # Client libraries (CSS, JS, React)
│       └── templates/      # Page templates
├── ui.content/             # Sample content
│   ├── pom.xml
│   └── src/main/content/jcr_root/
│       ├── content/        # Sample pages
│       ├── conf/           # Content Fragment Models
│       └── content/experience-fragments/  # Experience Fragments
├── ui.frontend/            # React 18 application
│   ├── pom.xml
│   ├── package.json
│   ├── webpack.config.js
│   ├── tsconfig.json
│   └── src/
│       ├── components/     # React components (11 components)
│       ├── hooks/          # Custom hooks (3 hooks)
│       ├── context/        # React Context (AEM Provider)
│       ├── utils/          # Utility functions
│       └── styles/         # React-specific CSS
├── ui.config/              # OSGi configurations
│   ├── pom.xml
│   └── src/main/content/jcr_root/apps/devinreactaem/osgiconfig/
│       ├── config/         # Default configs
│       └── config.author/  # Author-specific configs
└── all/                    # Aggregation package
    └── pom.xml
```

## Prerequisites

- **Java 11** (JDK)
- **Maven 3.6+**
- **Node.js 18+** and npm 9+
- **AEM as a Cloud Service SDK** (download from [Software Distribution](https://experience.adobe.com/#/downloads/content/software-distribution/en/aemcloud.html))

## Setup & Deployment

### 1. Start AEM Cloud SDK

```bash
# Start the AEM Author instance
java -jar aem-sdk-quickstart-*.jar -p 4502 -r author

# (Optional) Start the AEM Publish instance
java -jar aem-sdk-quickstart-*.jar -p 4503 -r publish
```

Wait for AEM to fully start (check http://localhost:4502).

### 2. Build and Deploy

```bash
# Build all modules and deploy to AEM Author
mvn clean install -PautoInstallSinglePackage

# Or deploy individual modules:
# Deploy only the OSGi bundle
mvn clean install -PautoInstallBundle -pl core

# Deploy only the UI components
mvn clean install -PautoInstallPackage -pl ui.apps

# Deploy only the content
mvn clean install -PautoInstallPackage -pl ui.content
```

### 3. Build Frontend (React)

```bash
cd ui.frontend
npm install
npm run build    # Production build
npm run dev      # Development watch mode
npm run start    # Dev server with AEM proxy (port 3000)
```

### 4. Access the Site

- **Author**: http://localhost:4502/content/devinreactaem/us/en.html
- **Sites Console**: http://localhost:4502/sites.html/content/devinreactaem
- **CRXDE Lite**: http://localhost:4502/crx/de
- **OSGi Console**: http://localhost:4502/system/console

## Features

### AEM Components (15+)

| Component | Description | React Enhanced |
|-----------|-------------|:-:|
| Hero Banner | Full-width hero with CTA | Yes |
| Teaser/Card | Content teasers with image | Yes |
| Card List | Grid/list of cards | Yes |
| Accordion | Expandable panels | Yes |
| Tabs | Tabbed content | Yes |
| Carousel | Auto-rotating slides | Yes |
| Navigation | Site navigation with mobile menu | No |
| Footer | Multi-column footer | No |
| Search | Full-text search | Yes |
| Content Fragment List | CF display component | Yes |
| Experience Fragment | XF embedding | Yes |
| Breadcrumb | Page trail | No |
| Social Share | Social media sharing | No |
| Form Container | Contact form with validation | Yes |
| Modal/Dialog | Accessible modal dialog | Yes |
| React Container | Generic React mount point | Yes |

### Java Backend

#### Sling Models
- `HeroModel` - Hero banner with alignment, overlay, background
- `TeaserModel` - Card/teaser with featured variant
- `AccordionModel` - Expandable panels with single/multi expand
- `TabsModel` - Horizontal/vertical tabs
- `CarouselModel` - Slides with autoplay settings
- `NavigationModel` - Multi-level navigation tree
- `SearchModel` - Search configuration
- `ContentFragmentListModel` - Content fragment querying

#### OSGi Services
- `ContentService` / `ContentServiceImpl` - Content CRUD operations
- `SearchService` / `SearchServiceImpl` - Full-text search via QueryBuilder
- `EmailService` / `EmailServiceImpl` - Email notification service

#### Sling Servlets
- `SearchServlet` - REST API for search (`/bin/devinreactaem/search`)
- `ContentFragmentServlet` - Content fragment API (`/bin/devinreactaem/contentfragments`)
- `FormSubmissionServlet` - Form handling (`/bin/devinreactaem/form`)

#### Schedulers
- `ContentSyncScheduler` - Syncs content every 6 hours
- `ReportGenerationScheduler` - Generates reports weekly on Monday

#### Event Listeners
- `PageEventListener` - Listens for page create/modify/delete events
- `ReplicationEventListener` - Monitors activation/deactivation events
- `ResourceChangeListener` - Watches JCR resource changes
- `DAMAssetListener` - Monitors DAM asset upload/modification events

#### Workflow Process Steps
- `ContentApprovalWorkflowProcess` - Content review and approval
- `AutoTaggingWorkflowProcess` - Automatic content tagging
- `NotificationWorkflowProcess` - Stakeholder notifications

#### Sling Jobs
- `EmailNotificationJobConsumer` - Async email sending
- `ContentSyncJobConsumer` - Async content synchronization
- `SlingJobCreatorService` - Utility to submit jobs

#### Filters
- `LoggingFilter` - Request/response logging with timing

### React Frontend

#### Components
All components use TypeScript, follow accessibility best practices (ARIA attributes, keyboard navigation), and integrate with AEM via data attributes for progressive enhancement.

#### Custom Hooks
- `useAEMContent` - Fetch AEM content via Sling Model JSON
- `useContentFragment` - Query and display content fragments
- `useSearch` - Debounced search with autocomplete

#### Architecture
- React 18 with `createRoot` API
- TypeScript strict mode
- Webpack 5 with code splitting
- AEM Context Provider for author mode detection
- MutationObserver for dynamic component initialization (AEM author mode)
- Progressive enhancement: HTL renders server-side, React hydrates client-side

### Content Fragment Models
- **Article** - Blog posts with title, author, date, summary, body, category, tags
- **Product** - Product catalog with name, SKU, price, description, stock status
- **Team Member** - Staff profiles with name, title, department, bio, social links

### Experience Fragments
- **Header** - Site header with navigation and search
- **Footer** - Site footer with social sharing

### OSGi Configurations
- Service user mappings with repo init
- Scheduler cron expressions
- Content service settings
- Search service settings
- Workflow package info provider
- Logging filter configuration

## Testing Features in AEM

### Test Components
1. Navigate to Sites Console: http://localhost:4502/sites.html/content/devinreactaem
2. Open a page in the editor
3. Drag and drop components from the side panel
4. Configure components via their dialogs

### Test Workflows
1. Go to Workflow Models: http://localhost:4502/libs/cq/workflow/admin/console/content/models.html
2. Create a workflow using the provided process steps
3. Start a workflow on content pages

### Test Content Fragments
1. Navigate to Assets: http://localhost:4502/assets.html/content/dam/devinreactaem
2. Create content fragments using the provided models
3. Use the Content Fragment List component to display them

### Test Experience Fragments
1. Go to Experience Fragments: http://localhost:4502/aem/experience-fragments.html/content/experience-fragments/devinreactaem
2. Edit the header/footer fragments
3. They will be included in page rendering

### Test Schedulers
1. Check OSGi Console: http://localhost:4502/system/console/configMgr
2. Search for "devinreactaem" to see scheduler configs
3. Adjust cron expressions for immediate testing

### Test Event Listeners
1. Create/modify/delete pages to trigger PageEventListener
2. Activate pages to trigger ReplicationEventListener
3. Upload DAM assets to trigger DAMAssetListener
4. Check logs: `tail -f crx-quickstart/logs/error.log | grep "devinreactaem"`

### Test Sling Jobs
1. Submit a form to trigger EmailNotificationJobConsumer
2. Check Sling Job console: http://localhost:4502/system/console/slingjobs
3. Monitor job execution in logs

### Test Search
1. Navigate to a page with the Search component
2. Type a query (minimum 2 characters)
3. Results are fetched via the SearchServlet REST API

### Test React Integration
1. Open browser DevTools console
2. Look for `[data-react-initialized]` elements
3. React components hydrate on top of server-rendered HTL markup
4. Test interactive features (accordion expand/collapse, tab switching, carousel navigation, modal open/close, form validation)

## Maven Profiles

| Profile | Description | Usage |
|---------|-------------|-------|
| `autoInstallSinglePackage` | Deploy all modules as single package | `mvn clean install -PautoInstallSinglePackage` |
| `autoInstallBundle` | Deploy only OSGi bundle | `mvn clean install -PautoInstallBundle -pl core` |
| `autoInstallPackage` | Deploy individual content package | `mvn clean install -PautoInstallPackage -pl ui.apps` |

## Configuration

Default configuration can be overridden via environment-specific OSGi configs:

- `ui.config/src/main/content/jcr_root/apps/devinreactaem/osgiconfig/config/` - Default
- `ui.config/src/main/content/jcr_root/apps/devinreactaem/osgiconfig/config.author/` - Author only
- `ui.config/src/main/content/jcr_root/apps/devinreactaem/osgiconfig/config.publish/` - Publish only

## Technology Stack

- **Backend**: Java 11, OSGi, Apache Sling, HTL/Sightly
- **Frontend**: React 18, TypeScript, Webpack 5
- **Build**: Maven 3.6+, frontend-maven-plugin
- **Target**: AEM as a Cloud Service SDK
