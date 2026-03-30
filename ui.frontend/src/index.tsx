import React from 'react';
import { createRoot } from 'react-dom/client';
import { AEMProvider } from './context/AEMContext';
import { HeroComponent } from './components/Hero/Hero';
import { TeaserComponent } from './components/Teaser/Teaser';
import { CardListComponent } from './components/CardList/CardList';
import { AccordionComponent } from './components/Accordion/Accordion';
import { TabsComponent } from './components/Tabs/Tabs';
import { CarouselComponent } from './components/Carousel/Carousel';
import { SearchComponent } from './components/Search/Search';
import { ModalComponent } from './components/Modal/Modal';
import { FormComponent } from './components/Form/Form';
import { ContentFragmentComponent } from './components/ContentFragment/ContentFragment';
import { ExperienceFragmentComponent } from './components/ExperienceFragment/ExperienceFragment';
import './styles/main.css';

interface ComponentMapping {
    selector: string;
    component: React.ComponentType<Record<string, string>>;
}

const componentMappings: ComponentMapping[] = [
    { selector: '[data-cmp-is="react-hero"]', component: HeroComponent },
    { selector: '[data-cmp-is="react-teaser"]', component: TeaserComponent },
    { selector: '[data-cmp-is="react-cardlist"]', component: CardListComponent },
    { selector: '[data-cmp-is="react-accordion"]', component: AccordionComponent },
    { selector: '[data-cmp-is="react-tabs"]', component: TabsComponent },
    { selector: '[data-cmp-is="react-carousel"]', component: CarouselComponent },
    { selector: '[data-cmp-is="react-search"]', component: SearchComponent },
    { selector: '[data-cmp-is="react-modal"]', component: ModalComponent },
    { selector: '[data-cmp-is="react-form"]', component: FormComponent },
    { selector: '[data-cmp-is="react-contentfragmentlist"]', component: ContentFragmentComponent },
    { selector: '[data-cmp-is="react-experiencefragment"]', component: ExperienceFragmentComponent },
];

function getDataAttributes(element: HTMLElement): Record<string, string> {
    const attrs: Record<string, string> = {};
    Array.from(element.attributes).forEach((attr) => {
        if (attr.name.startsWith('data-cmp-')) {
            const key = attr.name.replace('data-cmp-', '').replace(/-([a-z])/g, (_, c) => c.toUpperCase());
            attrs[key] = attr.value;
        }
    });
    return attrs;
}

function initializeComponents(): void {
    componentMappings.forEach(({ selector, component: Component }) => {
        const elements = document.querySelectorAll<HTMLElement>(selector);
        elements.forEach((element) => {
            if (element.dataset.reactInitialized) return;

            const props = getDataAttributes(element);
            const root = createRoot(element);
            root.render(
                <AEMProvider>
                    <Component {...props} />
                </AEMProvider>
            );
            element.dataset.reactInitialized = 'true';
        });
    });
}

// Initialize React app container if present
function initializeReactApp(): void {
    const appContainers = document.querySelectorAll<HTMLElement>('[data-cmp-is="react-app"]');
    appContainers.forEach((container) => {
        const rootId = container.dataset.cmpRootId;
        if (rootId) {
            const rootElement = document.getElementById(rootId);
            if (rootElement && !rootElement.dataset.reactInitialized) {
                const configStr = container.dataset.cmpConfig || '{}';
                let config: Record<string, unknown> = {};
                try {
                    config = JSON.parse(configStr);
                } catch (e) {
                    console.warn('Failed to parse React container config:', e);
                }

                const root = createRoot(rootElement);
                root.render(
                    <AEMProvider>
                        <div className="devinreactaem-app">
                            <h2>React Application Container</h2>
                            <p>Configuration: {JSON.stringify(config)}</p>
                        </div>
                    </AEMProvider>
                );
                rootElement.dataset.reactInitialized = 'true';
            }
        }
    });
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        initializeComponents();
        initializeReactApp();
    });
} else {
    initializeComponents();
    initializeReactApp();
}

// Observe for dynamically added components (for AEM author mode)
const observer = new MutationObserver(() => {
    initializeComponents();
    initializeReactApp();
});

observer.observe(document.body, {
    childList: true,
    subtree: true,
});

export { initializeComponents, initializeReactApp };
