import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Cog, Home, Package2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface LayoutProps {
  children: ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  const location = useLocation();
  const isHome = location.pathname === '/';

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="sticky top-0 z-50 w-full border-b border-gray-200 bg-white/95 backdrop-blur">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex h-14 items-center">
          <Link to="/" className="flex items-center gap-2.5 mr-8">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <Cog className="h-4.5 w-4.5" />
            </div>
            <div className="flex flex-col">
              <span className="text-base font-semibold leading-tight text-gray-900">SAP LO-VC#</span>
              <span className="text-[10px] text-gray-500 leading-tight">Configuration Manager</span>
            </div>
          </Link>

          <nav className="flex items-center gap-1">
            <Link
              to="/"
              className={cn(
                "flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors",
                isHome
                  ? "bg-indigo-50 text-indigo-700"
                  : "text-gray-600 hover:text-gray-900 hover:bg-gray-100"
              )}
            >
              <Home className="h-4 w-4" />
              Materials
            </Link>
          </nav>

          <div className="ml-auto flex items-center gap-4">
            <div className="flex items-center gap-1.5 text-xs text-gray-400">
              <Package2 className="h-3.5 w-3.5" />
              <span>Demo</span>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {children}
      </main>
    </div>
  );
}
