export interface WeightedEntry<T> {
  value: T;
  weight: number;
}

export function weightedRandom<T>(entries: Array<WeightedEntry<T>>): T {
  const filtered = entries.filter((entry) => entry.weight > 0);
  const total = filtered.reduce((sum, entry) => sum + entry.weight, 0);
  if (filtered.length === 0 || total <= 0) {
    throw new Error("No positive weight entries.");
  }

  let roll = Math.random() * total;
  for (const entry of filtered) {
    roll -= entry.weight;
    if (roll <= 0) {
      return entry.value;
    }
  }

  return filtered[filtered.length - 1].value;
}
