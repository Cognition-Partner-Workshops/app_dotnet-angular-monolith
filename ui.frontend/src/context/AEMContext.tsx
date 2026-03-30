import React, { createContext, useContext, useState, useCallback, ReactNode } from 'react';

interface AEMContextType {
    isAuthorMode: boolean;
    contentPath: string;
    locale: string;
    setContentPath: (path: string) => void;
    fetchContent: (path: string) => Promise<Record<string, unknown>>;
    isLoading: boolean;
}

const AEMContext = createContext<AEMContextType>({
    isAuthorMode: false,
    contentPath: '/content/devinreactaem',
    locale: 'en',
    setContentPath: () => {},
    fetchContent: async () => ({}),
    isLoading: false,
});

interface AEMProviderProps {
    children: ReactNode;
}

export function AEMProvider({ children }: AEMProviderProps): JSX.Element {
    const [contentPath, setContentPath] = useState('/content/devinreactaem');
    const [isLoading, setIsLoading] = useState(false);

    const isAuthorMode = typeof window !== 'undefined' &&
        (window.location.pathname.includes('/editor.html') ||
         document.querySelector('body.aem-AuthorLayer-Edit') !== null);

    const locale = typeof window !== 'undefined'
        ? document.documentElement.lang || 'en'
        : 'en';

    const fetchContent = useCallback(async (path: string): Promise<Record<string, unknown>> => {
        setIsLoading(true);
        try {
            const response = await fetch(`${path}.model.json`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            return data as Record<string, unknown>;
        } catch (error) {
            console.error('Failed to fetch AEM content:', error);
            return {};
        } finally {
            setIsLoading(false);
        }
    }, []);

    const value: AEMContextType = {
        isAuthorMode,
        contentPath,
        locale,
        setContentPath,
        fetchContent,
        isLoading,
    };

    return <AEMContext.Provider value={value}>{children}</AEMContext.Provider>;
}

export function useAEM(): AEMContextType {
    const context = useContext(AEMContext);
    if (!context) {
        throw new Error('useAEM must be used within an AEMProvider');
    }
    return context;
}

export default AEMContext;
