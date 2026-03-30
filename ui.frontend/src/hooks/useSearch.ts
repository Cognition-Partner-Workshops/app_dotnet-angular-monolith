import { useState, useCallback, useRef } from 'react';

interface SearchResult {
    path: string;
    title: string;
    excerpt: string;
}

interface UseSearchResult {
    results: SearchResult[];
    isSearching: boolean;
    error: string | null;
    totalResults: number;
    search: (query: string) => void;
    clearResults: () => void;
}

export function useSearch(
    apiUrl: string = '/bin/devinreactaem/search',
    options?: { searchRoot?: string; limit?: number; debounceMs?: number }
): UseSearchResult {
    const [results, setResults] = useState<SearchResult[]>([]);
    const [isSearching, setIsSearching] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [totalResults, setTotalResults] = useState(0);

    const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    const searchRoot = options?.searchRoot || '/content/devinreactaem';
    const limit = options?.limit || 10;
    const debounceMs = options?.debounceMs || 300;

    const performSearch = useCallback(async (query: string) => {
        if (!query || query.trim().length < 2) {
            setResults([]);
            setTotalResults(0);
            return;
        }

        setIsSearching(true);
        setError(null);

        try {
            const params = new URLSearchParams({
                q: query.trim(),
                root: searchRoot,
                limit: String(limit),
            });

            const response = await fetch(`${apiUrl}?${params}`, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' },
            });

            if (!response.ok) {
                throw new Error(`Search failed: ${response.status}`);
            }

            const data = await response.json();
            setResults(data.results || []);
            setTotalResults(data.total || 0);
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Search error';
            setError(message);
            setResults([]);
            setTotalResults(0);
        } finally {
            setIsSearching(false);
        }
    }, [apiUrl, searchRoot, limit]);

    const search = useCallback((query: string) => {
        if (debounceTimer.current) {
            clearTimeout(debounceTimer.current);
        }
        debounceTimer.current = setTimeout(() => performSearch(query), debounceMs);
    }, [performSearch, debounceMs]);

    const clearResults = useCallback(() => {
        setResults([]);
        setTotalResults(0);
        setError(null);
    }, []);

    return { results, isSearching, error, totalResults, search, clearResults };
}
