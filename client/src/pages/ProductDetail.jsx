import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ShoppingCart, Minus, Plus, ArrowLeft, Package } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import api from '../api';
import ProductCard from '../components/ProductCard';

export default function ProductDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const { addToCart } = useCart();
  const [product, setProduct] = useState(null);
  const [related, setRelated] = useState([]);
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setQuantity(1);
    api.get(`/products/${id}`)
      .then(res => {
        setProduct(res.data);
        setRelated(res.data.related || []);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [id]);

  const handleAddToCart = () => {
    if (!user) return;
    addToCart(product.id, quantity);
  };

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 text-lg">Product not found</p>
        <Link to="/products" className="btn-primary mt-4 inline-block">Back to Products</Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <Link to="/products" className="inline-flex items-center gap-1 text-gray-500 hover:text-primary-600 mb-6 text-sm font-medium">
        <ArrowLeft className="w-4 h-4" /> Back to Products
      </Link>

      <div className="grid md:grid-cols-2 gap-8 lg:gap-12">
        {/* Image */}
        <div className="aspect-square bg-gray-100 rounded-2xl overflow-hidden">
          <img
            src={product.image_url}
            alt={product.name}
            className="w-full h-full object-cover"
            onError={(e) => {
              e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(product.name)}&background=dcfce7&color=166534&size=600`;
            }}
          />
        </div>

        {/* Details */}
        <div className="flex flex-col">
          {product.category_name && (
            <span className="text-primary-600 font-medium text-sm mb-2">{product.category_name}</span>
          )}
          <h1 className="text-3xl font-bold text-gray-800 mb-4">{product.name}</h1>
          <p className="text-gray-600 leading-relaxed mb-6">{product.description}</p>

          <div className="flex items-baseline gap-2 mb-6">
            <span className="text-4xl font-extrabold text-primary-700">${product.price.toFixed(2)}</span>
            <span className="text-gray-400 text-lg">/ {product.unit}</span>
          </div>

          {/* Stock info */}
          <div className="flex items-center gap-2 mb-6">
            <Package className="w-5 h-5 text-gray-400" />
            {product.stock > 10 ? (
              <span className="text-green-600 font-medium">In Stock ({product.stock} available)</span>
            ) : product.stock > 0 ? (
              <span className="text-orange-600 font-medium">Low Stock ({product.stock} left)</span>
            ) : (
              <span className="text-red-600 font-medium">Out of Stock</span>
            )}
          </div>

          {/* Quantity & Add to Cart */}
          {user && product.stock > 0 && (
            <div className="flex flex-col sm:flex-row gap-4 mb-8">
              <div className="flex items-center border border-gray-300 rounded-lg">
                <button
                  onClick={() => setQuantity(Math.max(1, quantity - 1))}
                  className="p-3 hover:bg-gray-50 transition-colors"
                >
                  <Minus className="w-4 h-4" />
                </button>
                <span className="px-6 py-3 font-semibold text-center min-w-[60px]">{quantity}</span>
                <button
                  onClick={() => setQuantity(Math.min(product.stock, quantity + 1))}
                  className="p-3 hover:bg-gray-50 transition-colors"
                >
                  <Plus className="w-4 h-4" />
                </button>
              </div>
              <button onClick={handleAddToCart} className="btn-primary flex items-center justify-center gap-2 flex-1">
                <ShoppingCart className="w-5 h-5" /> Add to Cart
              </button>
            </div>
          )}

          {!user && (
            <div className="bg-primary-50 rounded-lg p-4 mb-8">
              <p className="text-primary-700 text-sm">
                <Link to="/login" className="font-semibold underline">Sign in</Link> or{' '}
                <Link to="/register" className="font-semibold underline">create an account</Link> to add items to your cart.
              </p>
            </div>
          )}

          {/* Product info */}
          <div className="border-t border-gray-200 pt-6 space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Category</span>
              <span className="font-medium">{product.category_name || 'Uncategorized'}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Unit</span>
              <span className="font-medium capitalize">{product.unit}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">SKU</span>
              <span className="font-medium">DF-{String(product.id).padStart(4, '0')}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Related Products */}
      {related.length > 0 && (
        <div className="mt-16">
          <h2 className="text-2xl font-bold text-gray-800 mb-6">Related Products</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 md:gap-6">
            {related.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
