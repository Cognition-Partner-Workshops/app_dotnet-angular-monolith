import { Link } from 'react-router-dom';
import { Milk, Mail, Phone, MapPin } from 'lucide-react';

export default function Footer() {
  return (
    <footer className="bg-gray-900 text-gray-300">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Brand */}
          <div className="col-span-1 md:col-span-2">
            <Link to="/" className="flex items-center gap-2 text-white font-bold text-xl mb-4">
              <Milk className="w-7 h-7 text-primary-400" />
              DairyFresh
            </Link>
            <p className="text-gray-400 text-sm leading-relaxed max-w-md">
              Your trusted source for fresh, high-quality dairy products. We deliver farm-fresh milk, cheese, 
              butter, yogurt, and more straight to your doorstep.
            </p>
          </div>

          {/* Quick Links */}
          <div>
            <h3 className="text-white font-semibold mb-4">Quick Links</h3>
            <div className="flex flex-col gap-2">
              <Link to="/products" className="text-sm hover:text-primary-400 transition-colors">All Products</Link>
              <Link to="/products?category=1" className="text-sm hover:text-primary-400 transition-colors">Fresh Milk</Link>
              <Link to="/products?category=2" className="text-sm hover:text-primary-400 transition-colors">Cheese</Link>
              <Link to="/products?category=4" className="text-sm hover:text-primary-400 transition-colors">Yogurt</Link>
            </div>
          </div>

          {/* Contact */}
          <div>
            <h3 className="text-white font-semibold mb-4">Contact Us</h3>
            <div className="flex flex-col gap-3">
              <div className="flex items-center gap-2 text-sm">
                <Phone className="w-4 h-4 text-primary-400" />
                +1 (555) 123-4567
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Mail className="w-4 h-4 text-primary-400" />
                hello@dairyfresh.com
              </div>
              <div className="flex items-center gap-2 text-sm">
                <MapPin className="w-4 h-4 text-primary-400" />
                123 Farm Road, Dairyville
              </div>
            </div>
          </div>
        </div>

        <div className="border-t border-gray-800 mt-8 pt-8 text-center text-sm text-gray-500">
          &copy; {new Date().getFullYear()} DairyFresh. All rights reserved. Fresh from the farm to your table.
        </div>
      </div>
    </footer>
  );
}
