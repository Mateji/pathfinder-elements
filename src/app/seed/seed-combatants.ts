import { Combatant } from '../models/combatant.model';

export const SEED_COMBATANTS: Combatant[] = [
    {
        id: 'pc-aria-01',
        name: 'Aria von Hohenquell',
        side: 'PC',
        initiative: 17,
        defense: {
            ac: 18,
            touch: 13,
            flatFooted: 15,
            fort: 5,
            ref: 6,
            will: 2
        },
        notes: 'Bogenschützin. Hat oft “Rapid Shot” aktiv.'
    },
    {
        id: 'pc-borin-01',
        name: 'Borin Eisenfaust',
        side: 'PC',
        initiative: 12,
        defense: {
            ac: 20,
            touch: 10,
            flatFooted: 19,
            fort: 7,
            ref: 1,
            will: 3
        },
        notes: 'Frontliner. Schild + schwere Rüstung.'
    },
    {
        id: 'pc-lysa-01',
        name: 'Lysa “Krähenblick” Morgenstern',
        side: 'PC',
        initiative: 12, // absichtlich gleich wie Borin (Tie-Case)
        defense: {
            ac: 16,
            touch: 14,
            flatFooted: 12,
            fort: 2,
            ref: 5,
            will: 6
        },
        notes: 'Zauberwirkerin. Kontrollzauber, wenig HP.'
    },

    {
        id: 'npc-goblin-01',
        name: 'Goblin Plänkler A',
        side: 'NPC',
        initiative: 15,
        defense: {
            ac: 16,
            touch: 14,
            flatFooted: 12,
            fort: 2,
            ref: 6,
            will: 0
        },
        attacks: [
            { label: 'Kurzschwert', bonus: 4, damage: '1d4+2' },
            { label: 'Wurfspieß', bonus: 5, damage: '1d6+2' }
        ],
        notes: 'Kleiner, nervöser Nahkämpfer.'
    },
    {
        id: 'npc-goblin-02',
        name: 'Goblin Plänkler B',
        side: 'NPC',
        initiative: 9,
        defense: {
            ac: 15,
            touch: 13,
            flatFooted: 12,
            fort: 1,
            ref: 5,
            will: -1
        },
        attacks: [{ label: 'Kurzschwert', bonus: 3, damage: '1d4+1' }]
    },
    {
        id: 'npc-goblin-03',
        name: 'Goblin Bogenschütze',
        side: 'NPC',
        initiative: 19,
        defense: {
            ac: 14,
            touch: 14,
            flatFooted: 10,
            fort: 1,
            ref: 6,
            will: 0
        },
        attacks: [
            { label: 'Kurzbogen', bonus: 6, damage: '1d4+1' },
            { label: 'Dolch', bonus: 2, damage: '1d3+1' }
        ],
        notes: 'Bleibt auf Abstand, sucht Deckung.'
    },
    {
        id: 'npc-goblin-04',
        name: 'Goblin Brandstifter mit viel zu langem Namen',
        side: 'NPC',
        initiative: 11,
        defense: {
            ac: 13,
            touch: 13,
            flatFooted: 11,
            fort: 0,
            ref: 5,
            will: 1
        },
        attacks: [
            { label: 'Alchemistisches Feuer', bonus: 5, damage: '1d6 fire' },
            { label: 'Facke', bonus: 3, damage: '1d4+1 fire' }
        ],
        notes: 'Guter Layout-Stresstest wegen langem Namen.'
    },

    {
        id: 'npc-orc-01',
        name: 'Ork Schläger',
        side: 'NPC',
        initiative: 14,
        defense: {
            ac: 15,
            touch: 9,
            flatFooted: 14,
            fort: 6,
            ref: 2,
            will: 1
        },
        attacks: [
            { label: 'Großaxt', bonus: 7, damage: '1d12+4' },
            { label: 'Wurfbeil', bonus: 4, damage: '1d6+4' }
        ],
        notes: 'Treffer tun weh. Gute Demo für “aktive” Angriffsanzeige.'
    },
    {
        id: 'npc-goblin-boss-01',
        name: 'Goblinboss “Grimzack”',
        side: 'NPC',
        initiative: 16,
        defense: {
            ac: 17,
            touch: 15,
            flatFooted: 12,
            fort: 3,
            ref: 7,
            will: 2
        },
        attacks: [
            { label: 'Krummsäbel', bonus: 6, damage: '1d6+2' },
            { label: 'Hieb (Off-Hand)', bonus: 4, damage: '1d4+1' }
        ],
        notes: 'Hat Tricks, versucht zu flankieren.'
    },

    {
        id: 'npc-goblin-05',
        name: 'Goblin Läufer (keine Angriffe eingetragen)',
        side: 'NPC',
        initiative: 8,
        defense: {
            ac: 15,
            touch: 15,
            flatFooted: 11,
            fort: 1,
            ref: 7,
            will: 0
        },
        notes: 'Absichtlich ohne attacks, um “Keine Angriffe” UI zu testen.'
    }
];
