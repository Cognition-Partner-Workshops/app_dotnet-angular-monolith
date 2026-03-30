import React, { useEffect, useState } from 'react';
import { useAEMContent } from '../../hooks/useAEMContent';

interface ExperienceFragmentProps {
    fragmentPath?: string;
    [key: string]: string | undefined;
}

interface XFContent {
    ':items'?: Record<string, { ':type': string; text?: string; [key: string]: unknown }>;
    title?: string;
}

export function ExperienceFragmentComponent({ fragmentPath }: ExperienceFragmentProps): JSX.Element {
    const [htmlContent, setHtmlContent] = useState<string>('');
    const { data, isLoading, error } = useAEMContent<XFContent>(
        fragmentPath || '/content/experience-fragments/devinreactaem/header/master'
    );

    useEffect(() => {
        if (data) {
            const items = data[':items'];
            if (items) {
                const html = Object.values(items)
                    .map(item => item.text || '')
                    .filter(Boolean)
                    .join('');
                setHtmlContent(html);
            }
        }
    }, [data]);

    if (isLoading) {
        return (
            <div className="react-loading">
                <div className="react-loading__spinner" />
                <span>Loading experience fragment...</span>
            </div>
        );
    }

    if (error) {
        return <div className="react-xf-error">Unable to load experience fragment</div>;
    }

    if (htmlContent) {
        return <div className="react-xf" dangerouslySetInnerHTML={{ __html: htmlContent }} />;
    }

    return (
        <div className="react-xf react-xf--placeholder">
            <p>Experience Fragment: {fragmentPath || 'No path specified'}</p>
        </div>
    );
}
