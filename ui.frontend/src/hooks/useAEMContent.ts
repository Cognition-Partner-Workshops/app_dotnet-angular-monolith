import { useState, useEffect, useCallback } from 'react';

interface UseAEMContentResult<T> {
    data: T | null;
    isLoading: boolean;
    error: string | null;
    refetch: () => void;
}

export function useAEMContent<T = Record<string, unknown>>(
    path: string,
    options?: { autoFetch?: boolean; selector?: string }
): UseAEMContentResult<T> {
    const [data, setData] = useState<T | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const selector = options?.selector || 'model';
    const autoFetch = options?.autoFetch !== false;

    const fetchData = useCallback(async () => {
        if (!path) return;
        setIsLoading(true);
        setError(null);
        try {
            const url = `${path}.${selector}.json`;
            const response = await fetch(url, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' },
            });
            if (!response.ok) {
                throw new Error(`Failed to fetch content: ${response.status} ${response.statusText}`);
            }
            const result = await response.json();
            setData(result as T);
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Unknown error';
            setError(message);
            console.error(`useAEMContent error for path ${path}:`, err);
        } finally {
            setIsLoading(false);
        }
    }, [path, selector]);

    useEffect(() => {
        if (autoFetch) {
            fetchData();
        }
    }, [autoFetch, fetchData]);

    return { data, isLoading, error, refetch: fetchData };
}
