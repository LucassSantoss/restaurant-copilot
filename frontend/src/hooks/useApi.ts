import { useEffect, useState } from 'react'

/** Loads data once (and every <paramref name="refreshMs"/> when given), ignoring results after unmount. */
export function useApi<T>(load: () => Promise<T>, refreshMs?: number) {
  const [data, setData] = useState<T | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let active = true
    const run = () =>
      load()
        .then((value) => {
          if (active) {
            setData(value)
            setFailed(false)
          }
        })
        .catch(() => active && setFailed(true))

    run()
    const timer = refreshMs ? setInterval(run, refreshMs) : undefined
    return () => {
      active = false
      clearInterval(timer)
    }
  }, [load, refreshMs])

  return { data, failed }
}
