import React, { useState, useCallback } from 'react';

interface TabsProps {
    activeTab?: string;
    orientation?: string;
    [key: string]: string | undefined;
}

export function TabsComponent({ orientation = 'horizontal' }: TabsProps): JSX.Element {
    const [activeIndex, setActiveIndex] = useState(0);

    const tabs = [
        { id: 'overview', title: 'Overview', content: 'DevinReactAEM is a comprehensive AEM project with React integration.' },
        { id: 'features', title: 'Features', content: 'Includes custom components, workflows, content fragments, and more.' },
        { id: 'tech', title: 'Technology', content: 'Built with React 18, TypeScript, Java 11, HTL/Sightly, and OSGi.' },
    ];

    const handleTabClick = useCallback((index: number) => {
        setActiveIndex(index);
    }, []);

    const handleKeyDown = useCallback((e: React.KeyboardEvent, index: number) => {
        const isHorizontal = orientation === 'horizontal';
        const prevKey = isHorizontal ? 'ArrowLeft' : 'ArrowUp';
        const nextKey = isHorizontal ? 'ArrowRight' : 'ArrowDown';

        if (e.key === prevKey) {
            e.preventDefault();
            setActiveIndex(prev => (prev - 1 + tabs.length) % tabs.length);
        } else if (e.key === nextKey) {
            e.preventDefault();
            setActiveIndex(prev => (prev + 1) % tabs.length);
        }
    }, [orientation, tabs.length]);

    return (
        <div className={`react-tabs react-tabs--${orientation}`}>
            <div className="react-tabs__tablist" role="tablist" aria-label="Content tabs">
                {tabs.map((tab, index) => (
                    <button
                        key={tab.id}
                        className={`react-tabs__tab ${index === activeIndex ? 'react-tabs__tab--active' : ''}`}
                        role="tab"
                        aria-selected={index === activeIndex}
                        aria-controls={`tabpanel-${tab.id}`}
                        id={`tab-${tab.id}`}
                        tabIndex={index === activeIndex ? 0 : -1}
                        onClick={() => handleTabClick(index)}
                        onKeyDown={(e) => handleKeyDown(e, index)}
                    >
                        {tab.title}
                    </button>
                ))}
            </div>
            {tabs.map((tab, index) => (
                <div
                    key={tab.id}
                    id={`tabpanel-${tab.id}`}
                    className="react-tabs__panel"
                    role="tabpanel"
                    aria-labelledby={`tab-${tab.id}`}
                    hidden={index !== activeIndex}
                >
                    <p>{tab.content}</p>
                </div>
            ))}
        </div>
    );
}
