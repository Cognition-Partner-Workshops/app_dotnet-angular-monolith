import React from 'react';
import { useContentFragment } from '../../hooks/useContentFragment';

interface ContentFragmentProps {
    fragmentPath?: string;
    modelPath?: string;
    [key: string]: string | undefined;
}

export function ContentFragmentComponent({ fragmentPath, modelPath }: ContentFragmentProps): JSX.Element {
    const { fragment, fragments, isLoading, error } = useContentFragment(
        fragmentPath || '/content/dam/devinreactaem',
        { single: !!fragmentPath, model: modelPath }
    );

    if (isLoading) {
        return (
            <div className="react-loading">
                <div className="react-loading__spinner" />
                <span>Loading content fragments...</span>
            </div>
        );
    }

    if (error) {
        return <div className="react-cf-error">Unable to load content: {error}</div>;
    }

    if (fragment) {
        return (
            <div className="react-cf-card">
                <h3 className="react-cf-card__title">{fragment.title}</h3>
                {fragment.description && <p className="react-cf-card__body">{fragment.description}</p>}
                {fragment.elements && Object.entries(fragment.elements).map(([key, element]) => (
                    <div key={key} className="react-cf-card__field">
                        <strong>{element.name}: </strong>
                        <span>{element.value}</span>
                    </div>
                ))}
            </div>
        );
    }

    return (
        <div className="react-cf-list">
            {fragments.length === 0 ? (
                <p>No content fragments found.</p>
            ) : (
                <div className="react-cf-list__grid">
                    {fragments.map((cf, index) => (
                        <div key={index} className="react-cf-card">
                            <h3 className="react-cf-card__title">{cf.title}</h3>
                            <p className="react-cf-card__meta">{cf.path}</p>
                            {cf.description && <p className="react-cf-card__body">{cf.description}</p>}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
