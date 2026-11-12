/** Slowly drifting warm light behind the interface. Purely decorative. */
export function AnimatedBackground() {
  return (
    <div aria-hidden className="pointer-events-none fixed inset-0 -z-10 overflow-hidden">
      <div className="absolute -left-40 -top-40 h-[38rem] w-[38rem] rounded-full bg-tomato-500/30 blur-[110px] animate-drift" />
      <div
        className="absolute -right-32 top-1/3 h-[32rem] w-[32rem] rounded-full bg-saffron-300/15 blur-[110px] animate-drift"
        style={{ animationDelay: '-8s' }}
      />
      <div
        className="absolute bottom-[-12rem] left-1/3 h-[30rem] w-[30rem] rounded-full bg-basil-500/15 blur-[110px] animate-drift"
        style={{ animationDelay: '-15s' }}
      />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,transparent_0%,var(--color-ink-950)_75%)]" />
    </div>
  )
}
