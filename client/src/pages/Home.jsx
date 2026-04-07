import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, Truck, Shield, Leaf, Clock } from 'lucide-react';
import api from '../api';
import ProductCard from '../components/ProductCard';

export default function Home() {
  const [featured, setFeatured] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      api.get('/products/featured'),
      api.get('/products/categories'),
    ])
      .then(([productsRes, categoriesRes]) => {
        setFeatured(productsRes.data);
        setCategories(categoriesRes.data);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-primary-600 via-primary-700 to-primary-800 text-white overflow-hidden">
        <div className="absolute inset-0 bg-[url('https://images.unsplash.com/photo-1628088062854-d1870b4553da?w=1200')] bg-cover bg-center opacity-15"></div>
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 md:py-28">
          <div className="max-w-2xl">
            <span className="inline-block bg-primary-500/30 backdrop-blur-sm text-primary-100 text-sm font-medium px-4 py-1.5 rounded-full mb-6">
              Farm Fresh Dairy Products
            </span>
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-extrabold leading-tight mb-6">
              Fresh From The <span className="text-primary-200">Farm</span> To Your Table
            </h1>
            <p className="text-lg text-primary-100 mb-8 leading-relaxed">
              Discover the finest quality dairy products sourced directly from local farms. 
              Fresh milk, artisanal cheese, creamy butter, and more delivered to your doorstep.
            </p>
            <div className="flex flex-wrap gap-4">
              <Link to="/products" className="inline-flex items-center gap-2 bg-white text-primary-700 font-bold py-3 px-8 rounded-lg hover:bg-primary-50 transition-all shadow-lg hover:shadow-xl">
                Shop Now <ArrowRight className="w-5 h-5" />
              </Link>
              <Link to="/register" className="inline-flex items-center gap-2 bg-primary-500/30 backdrop-blur-sm text-white font-bold py-3 px-8 rounded-lg hover:bg-primary-500/50 transition-all border border-primary-400/30">
                Create Account
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-12 bg-white border-b border-gray-100">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 md:gap-8">
            {[
              { icon: Truck, title: 'Free Delivery', desc: 'On orders above $30' },
              { icon: Leaf, title: 'Farm Fresh', desc: '100% natural products' },
              { icon: Shield, title: 'Quality Assured', desc: 'Lab tested & certified' },
              { icon: Clock, title: 'Same Day', desc: 'Quick delivery service' },
            ].map(({ icon: Icon, title, desc }) => (
              <div key={title} className="text-center">
                <div className="inline-flex items-center justify-center w-12 h-12 bg-primary-50 text-primary-600 rounded-xl mb-3">
                  <Icon className="w-6 h-6" />
                </div>
                <h3 className="font-semibold text-gray-800 text-sm md:text-base">{title}</h3>
                <p className="text-xs md:text-sm text-gray-500 mt-1">{desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Categories */}
      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-10">
            <h2 className="text-3xl font-bold text-gray-800">Shop by Category</h2>
            <p className="text-gray-500 mt-2">Browse our wide range of dairy products</p>
          </div>
          {loading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  to={`/products?category=${cat.id}`}
                  className="group card p-4 text-center hover:border-primary-200"
                >
                  <div className="w-20 h-20 mx-auto mb-3 rounded-full overflow-hidden bg-gray-100">
                    <img
                      src={cat.image_url}
                      alt={cat.name}
                      className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-300"
                      onError={(e) => {
                        e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(cat.name)}&background=dcfce7&color=166534&size=200`;
                      }}
                    />
                  </div>
                  <h3 className="font-semibold text-sm text-gray-800 group-hover:text-primary-600 transition-colors">{cat.name}</h3>
                  <p className="text-xs text-gray-400 mt-1">{cat.product_count} products</p>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Featured Products */}
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-10">
            <div>
              <h2 className="text-3xl font-bold text-gray-800">Featured Products</h2>
              <p className="text-gray-500 mt-2">Handpicked fresh dairy for you</p>
            </div>
            <Link to="/products" className="hidden md:inline-flex items-center gap-1 text-primary-600 font-semibold hover:text-primary-700">
              View All <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
          {loading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
              {featured.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          )}
          <div className="text-center mt-8 md:hidden">
            <Link to="/products" className="btn-primary">View All Products</Link>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-16 bg-gradient-to-r from-primary-50 to-green-50">
        <div className="max-w-3xl mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold text-gray-800 mb-4">Ready to Order Fresh Dairy?</h2>
          <p className="text-gray-600 mb-8">Join thousands of happy customers who trust DairyFresh for their daily dairy needs.</p>
          <Link to="/register" className="btn-primary text-lg !py-3 !px-10">Get Started Today</Link>
        </div>
      </section>
    </div>
  );
}
