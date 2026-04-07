import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Search, SlidersHorizontal, X } from 'lucide-react';
import api from '../api';
import ProductCard from '../components/ProductCard';

export default function Products() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState(searchParams.get('search') || '');
  const [selectedCategory, setSelectedCategory] = useState(searchParams.get('category') || '');
  const [sort, setSort] = useState(searchParams.get('sort') || '');
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    api.get('/products/categories').then(res => setCategories(res.data)).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    if (selectedCategory) params.set('category', selectedCategory);
    if (sort) params.set('sort', sort);

    api.get(`/products?${params.toString()}`)
      .then(res => setProducts(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [search, selectedCategory, sort]);

  const handleSearch = (e) => {
    e.preventDefault();
    const value = e.target.elements.search.value;
    setSearch(value);
    const params = new URLSearchParams(searchParams);
    if (value) params.set('search', value); else params.delete('search');
    setSearchParams(params);
  };

  const handleCategoryChange = (catId) => {
    const value = catId === selectedCategory ? '' : catId;
    setSelectedCategory(value);
    const params = new URLSearchParams(searchParams);
    if (value) params.set('category', value); else params.delete('category');
    setSearchParams(params);
  };

  const clearFilters = () => {
    setSearch('');
    setSelectedCategory('');
    setSort('');
    setSearchParams({});
  };

  const hasFilters = search || selectedCategory || sort;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-800">Our Products</h1>
        <p className="text-gray-500 mt-1">Browse our collection of fresh dairy products</p>
      </div>

      {/* Search & Filters Bar */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <form onSubmit={handleSearch} className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
          <input
            name="search"
            type="text"
            defaultValue={search}
            placeholder="Search products..."
            className="input-field !pl-10"
          />
        </form>
        <div className="flex gap-3">
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value)}
            className="input-field !w-auto"
          >
            <option value="">Sort by: Latest</option>
            <option value="price_asc">Price: Low to High</option>
            <option value="price_desc">Price: High to Low</option>
            <option value="name">Name: A-Z</option>
          </select>
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="btn-secondary !py-2 flex items-center gap-2 md:hidden"
          >
            <SlidersHorizontal className="w-4 h-4" /> Filters
          </button>
        </div>
      </div>

      <div className="flex gap-8">
        {/* Sidebar Filters - Desktop */}
        <div className={`${showFilters ? 'fixed inset-0 z-50 bg-black/50 md:relative md:bg-transparent' : 'hidden'} md:block`}>
          <div className={`${showFilters ? 'absolute right-0 top-0 h-full w-72 bg-white p-6 shadow-xl md:shadow-none md:relative md:w-auto md:p-0' : ''} w-56 shrink-0`}>
            {showFilters && (
              <div className="flex items-center justify-between mb-4 md:hidden">
                <h3 className="font-bold text-lg">Filters</h3>
                <button onClick={() => setShowFilters(false)}><X className="w-5 h-5" /></button>
              </div>
            )}
            <div className="card p-5">
              <h3 className="font-semibold text-gray-800 mb-3">Categories</h3>
              <div className="flex flex-col gap-2">
                <button
                  onClick={() => handleCategoryChange('')}
                  className={`text-left text-sm px-3 py-2 rounded-lg transition-colors ${
                    !selectedCategory ? 'bg-primary-50 text-primary-700 font-medium' : 'text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  All Products
                </button>
                {categories.map((cat) => (
                  <button
                    key={cat.id}
                    onClick={() => handleCategoryChange(String(cat.id))}
                    className={`text-left text-sm px-3 py-2 rounded-lg transition-colors ${
                      selectedCategory === String(cat.id) ? 'bg-primary-50 text-primary-700 font-medium' : 'text-gray-600 hover:bg-gray-50'
                    }`}
                  >
                    {cat.name} <span className="text-gray-400">({cat.product_count})</span>
                  </button>
                ))}
              </div>
            </div>
            {hasFilters && (
              <button onClick={clearFilters} className="mt-3 text-sm text-red-600 hover:text-red-700 font-medium">
                Clear all filters
              </button>
            )}
          </div>
        </div>

        {/* Products Grid */}
        <div className="flex-1">
          {loading ? (
            <div className="flex justify-center py-20">
              <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
            </div>
          ) : products.length === 0 ? (
            <div className="text-center py-20">
              <p className="text-gray-500 text-lg">No products found</p>
              {hasFilters && (
                <button onClick={clearFilters} className="mt-4 text-primary-600 font-medium hover:text-primary-700">
                  Clear filters
                </button>
              )}
            </div>
          ) : (
            <>
              <p className="text-sm text-gray-500 mb-4">{products.length} product{products.length !== 1 ? 's' : ''} found</p>
              <div className="grid grid-cols-2 lg:grid-cols-3 gap-4 md:gap-6">
                {products.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
