import React from 'react';

interface CardListProps {
    layout?: string;
    [key: string]: string | undefined;
}

export function CardListComponent({ layout = 'grid' }: CardListProps): JSX.Element {
    return (
        <div className={`react-cardlist react-cardlist--${layout}`}>
            <div className="react-cardlist__info">
                <p>Card list component - renders cards dynamically from AEM content</p>
            </div>
        </div>
    );
}
