import { Link } from 'react-router-dom';
import { ShoppingCart, Eye } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

export default function ProductCard({ product }) {
  const { user } = useAuth();
  const { addToCart } = useCart();

  const handleAddToCart = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (!user) return;
    addToCart(product.id);
  };

  return (
    <Link to={`/products/${product.id}`} className="card group">
      <div className="relative overflow-hidden aspect-square bg-gray-100">
        <img
          src={product.image_url}
          alt={product.name}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          onError={(e) => {
            e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(product.name)}&background=dcfce7&color=166534&size=400`;
          }}
        />
        {product.stock < 10 && product.stock > 0 && (
          <span className="absolute top-2 left-2 badge bg-orange-100 text-orange-700">Low Stock</span>
        )}
        {product.stock === 0 && (
          <span className="absolute top-2 left-2 badge bg-red-100 text-red-700">Out of Stock</span>
        )}
        {product.category_name && (
          <span className="absolute top-2 right-2 badge bg-primary-100 text-primary-700">{product.category_name}</span>
        )}
      </div>
      <div className="p-4">
        <h3 className="font-semibold text-gray-800 group-hover:text-primary-600 transition-colors line-clamp-1">
          {product.name}
        </h3>
        <p className="text-sm text-gray-500 mt-1 line-clamp-2">{product.description}</p>
        <div className="flex items-center justify-between mt-3">
          <div>
            <span className="text-lg font-bold text-primary-700">${product.price.toFixed(2)}</span>
            <span className="text-xs text-gray-400 ml-1">/ {product.unit}</span>
          </div>
          {user && product.stock > 0 && (
            <button
              onClick={handleAddToCart}
              className="p-2 bg-primary-50 text-primary-600 rounded-lg hover:bg-primary-100 transition-colors"
              title="Add to cart"
            >
              <ShoppingCart className="w-5 h-5" />
            </button>
          )}
        </div>
      </div>
    </Link>
  );
}
