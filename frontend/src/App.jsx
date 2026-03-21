import { Button } from '@/components/ui/button'

function App() {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <section className="mx-auto flex min-h-screen max-w-5xl items-center justify-center px-6">
        <div className="w-full rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          <p className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">
            GDG Hackathon
          </p>
          <h1 className="text-3xl font-bold">Task Management Frontend</h1>
          <p className="mt-3 text-slate-600">
            Tailwind CSS and shadcn are configured and ready.
          </p>
          <div className="mt-6">
            <Button>Create First Task</Button>
          </div>
        </div>
      </section>
    </main>
  )
}

export default App
