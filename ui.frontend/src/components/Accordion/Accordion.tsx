import React, { useState, useCallback } from 'react';

interface AccordionProps {
    singleExpansion?: string;
    [key: string]: string | undefined;
}

export function AccordionComponent({ singleExpansion }: AccordionProps): JSX.Element {
    const [expandedItems, setExpandedItems] = useState<Set<number>>(new Set());
    const isSingle = singleExpansion === 'true';

    const toggleItem = useCallback((index: number) => {
        setExpandedItems(prev => {
            const next = new Set(isSingle ? [] : prev);
            if (prev.has(index)) {
                next.delete(index);
            } else {
                next.add(index);
            }
            return next;
        });
    }, [isSingle]);

    const items = [
        { title: 'Getting Started', content: 'Learn how to set up and deploy the DevinReactAEM project to your local AEM Cloud SDK instance.' },
        { title: 'Component Development', content: 'Create custom AEM components with HTL/Sightly templates, Sling Models, and React interactivity.' },
        { title: 'Workflow Configuration', content: 'Configure content approval workflows, auto-tagging, and notification processes.' },
    ];

    return (
        <div className="react-accordion" role="presentation">
            {items.map((item, index) => {
                const isExpanded = expandedItems.has(index);
                const panelId = `accordion-panel-${index}`;
                const headerId = `accordion-header-${index}`;

                return (
                    <div key={index} className="react-accordion__item">
                        <h3 className="react-accordion__header">
                            <button
                                id={headerId}
                                className="react-accordion__button"
                                type="button"
                                aria-expanded={isExpanded}
                                aria-controls={panelId}
                                onClick={() => toggleItem(index)}
                            >
                                <span className="react-accordion__title">{item.title}</span>
                                <span className={`react-accordion__icon ${isExpanded ? 'react-accordion__icon--expanded' : ''}`}>
                                    {isExpanded ? '−' : '+'}
                                </span>
                            </button>
                        </h3>
                        <div
                            id={panelId}
                            className="react-accordion__panel"
                            role="region"
                            aria-labelledby={headerId}
                            hidden={!isExpanded}
                        >
                            <div className="react-accordion__content">
                                <p>{item.content}</p>
                            </div>
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
