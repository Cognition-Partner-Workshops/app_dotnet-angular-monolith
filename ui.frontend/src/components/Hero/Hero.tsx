import React from 'react';

interface HeroProps {
    title?: string;
    subtitle?: string;
    ctaText?: string;
    ctaLink?: string;
    [key: string]: string | undefined;
}

export function HeroComponent({ title, subtitle, ctaText, ctaLink }: HeroProps): JSX.Element | null {
    if (!title) return null;

    return (
        <div className="react-hero">
            <div className="react-hero__content">
                <h1 className="react-hero__title">{title}</h1>
                {subtitle && <p className="react-hero__subtitle">{subtitle}</p>}
                {ctaText && ctaLink && (
                    <a className="react-hero__cta" href={ctaLink}>
                        {ctaText}
                        <span className="react-hero__cta-arrow" aria-hidden="true"> &rarr;</span>
                    </a>
                )}
            </div>
        </div>
    );
}
