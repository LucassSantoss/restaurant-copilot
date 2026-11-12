import { ChefHat } from 'lucide-react'

export function BrandMark({ compact = false }: { compact?: boolean }) {
  return (
    <div className="flex items-center gap-3">
      <div className="relative grid h-10 w-10 place-items-center rounded-2xl bg-gradient-to-br from-tomato-400 to-tomato-500 shadow-lg shadow-tomato-500/30">
        <ChefHat className="h-5 w-5 text-cream-50" strokeWidth={2.2} />
      </div>
      {!compact && (
        <div className="leading-tight">
          <p className="font-display text-lg font-semibold tracking-tight">RestaurantCopilot</p>
          <p className="text-xs text-cream-500">Análises do seu restaurante com IA</p>
        </div>
      )}
    </div>
  )
}
