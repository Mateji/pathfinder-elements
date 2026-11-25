export type Language = 'de' | 'en';

interface Messages {
    readonly language: string;
    readonly initiativeTracker: string;
    readonly noCombatants: string;
    readonly previousTurn: string;
    readonly nextTurn: string;
    readonly armorClass: (value: number) => string;
}

export const messages: Readonly<Record<Language, Messages>> = {
    de: {
        language: 'Sprache',
        initiativeTracker: 'Initiative-Tracker',
        noCombatants: 'Keine Kampfteilnehmer verfügbar.',
        previousTurn: 'Vorheriger Zug',
        nextTurn: 'Nächster Zug',
        armorClass: (value) => {
            return `Rüstungsklasse ${value}`;
        }
    },
    en: {
        language: 'Language',
        initiativeTracker: 'Initiative Tracker',
        noCombatants: 'No combatants available.',
        previousTurn: 'Previous turn',
        nextTurn: 'Next turn',
        armorClass: (value) => {
            return `Armor Class ${value}`;
        }
    }
};
