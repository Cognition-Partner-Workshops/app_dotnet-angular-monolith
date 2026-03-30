import React from 'react';
import { getAEMPageUrl } from '../../utils/aemUtils';

interface TeaserProps {
    title?: string;
    description?: string;
    image?: string;
    link?: string;
    [key: string]: string | undefined;
}

export function TeaserComponent({ title, description, image, link }: TeaserProps): JSX.Element | null {
    if (!title) return null;

    return (
        <article className="react-teaser">
            {image && (
                <div className="react-teaser__image">
                    <img src={image} alt={title} loading="lazy" />
                </div>
            )}
            <div className="react-teaser__content">
                <h3 className="react-teaser__title">
                    {link ? <a href={getAEMPageUrl(link)}>{title}</a> : title}
                </h3>
                {description && <p className="react-teaser__description">{description}</p>}
            </div>
        </article>
    );
}
