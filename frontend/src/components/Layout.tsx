import { Link, Outlet } from 'react-router'

export default function Layout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <Link to="/" className="text-lg font-semibold">
            Cash-Flow Calendar
          </Link>
        </div>
      </header>

      <div className="bg-amber-50 text-amber-900">
        <p className="mx-auto max-w-5xl px-4 py-2 text-sm">
          Demo with sample data only. Please do not enter real financial information.
        </p>
      </div>

      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}