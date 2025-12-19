export type Side = 'PC' | 'NPC';

export interface Defense {
  ac: number;
  touch?: number;
  flatFooted?: number;
  fort?: number;
  ref?: number;
  will?: number;
}

export interface Attack {
  label: string; // z.B. "Biss"
  bonus: number; // z.B. +7
  damage?: string; // "1d6+4"
}

export interface Combatant {
  id: string;
  name: string;
  side: Side;

  initiative: number; // fürs MVP direkt als finaler Wert (später: roll + mod)
  defense: Defense;

  attacks?: Attack[]; // erstmal nur bei NPCs befüllen
  notes?: string;
}
