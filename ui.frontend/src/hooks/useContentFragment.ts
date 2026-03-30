import { useState, useEffect, useCallback } from 'react';

interface ContentFragmentElement {
    name: string;
    value: string;
    type: string;
}

interface ContentFragmentData {
    title: string;
    description: string;
    path: string;
    elements: Record<string, ContentFragmentElement>;
}

interface UseContentFragmentResult {
    fragment: ContentFragmentData | null;
    fragments: ContentFragmentData[];
    isLoading: boolean;
    error: string | null;
    refetch: () => void;
}

export function useContentFragment(
    path: string,
    options?: { single?: boolean; model?: string; limit?: number }
): UseContentFragmentResult {
    const [fragment, setFragment] = useState<ContentFragmentData | null>(null);
    const [fragments, setFragments] = useState<ContentFragmentData[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const single = options?.single || false;
    const model = options?.model;
    const limit = options?.limit || 10;

    const fetchData = useCallback(async () => {
        if (!path) return;
        setIsLoading(true);
        setError(null);

        try {
            const params = new URLSearchParams({ path });
            if (single) params.set('action', 'single');
            if (model) params.set('model', model);
            params.set('limit', String(limit));

            const response = await fetch(`/bin/devinreactaem/contentfragments?${params}`, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' },
            });

            if (!response.ok) {
                throw new Error(`Failed to fetch content fragments: ${response.status}`);
            }

            const data = await response.json();

            if (single) {
                setFragment(data as ContentFragmentData);
            } else {
                setFragments((data.fragments || []) as ContentFragmentData[]);
            }
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Unknown error';
            setError(message);
        } finally {
            setIsLoading(false);
        }
    }, [path, single, model, limit]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { fragment, fragments, isLoading, error, refetch: fetchData };
}
