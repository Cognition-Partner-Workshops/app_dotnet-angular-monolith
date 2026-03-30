import React, { useState, useCallback } from 'react';
import { useSearch } from '../../hooks/useSearch';

interface SearchProps {
    searchRoot?: string;
    [key: string]: string | undefined;
}

export function SearchComponent({ searchRoot }: SearchProps): JSX.Element {
    const [query, setQuery] = useState('');
    const { results, isSearching, error, totalResults, search, clearResults } = useSearch(
        '/bin/devinreactaem/search',
        { searchRoot: searchRoot || '/content/devinreactaem' }
    );

    const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        setQuery(value);
        if (value.length >= 2) {
            search(value);
        } else {
            clearResults();
        }
    }, [search, clearResults]);

    const handleSubmit = useCallback((e: React.FormEvent) => {
        e.preventDefault();
        if (query.length >= 2) {
            search(query);
        }
    }, [query, search]);

    return (
        <div className="react-search">
            <form className="react-search__form" onSubmit={handleSubmit} role="search">
                <label htmlFor="react-search-input" className="sr-only">Search</label>
                <input
                    id="react-search-input"
                    className="react-search__input"
                    type="search"
                    placeholder="Search content..."
                    value={query}
                    onChange={handleInputChange}
                    aria-label="Search content"
                />
                <button type="submit" className="react-search__button" disabled={isSearching}>
                    {isSearching ? 'Searching...' : 'Search'}
                </button>
            </form>

            {error && <div className="react-search__error" role="alert">{error}</div>}

            {results.length > 0 && (
                <div className="react-search__results">
                    <p className="react-search__count">{totalResults} result{totalResults !== 1 ? 's' : ''} found</p>
                    <ul className="react-search__list">
                        {results.map((result, index) => (
                            <li key={index} className="react-search__result">
                                <a href={`${result.path}.html`} className="react-search__result-link">
                                    <span className="react-search__result-title">{result.title}</span>
                                    {result.excerpt && (
                                        <span className="react-search__result-excerpt">{result.excerpt}</span>
                                    )}
                                </a>
                            </li>
                        ))}
                    </ul>
                </div>
            )}
        </div>
    );
}
