export type TemplateKind = 'characters' | 'monsters';

export const statisticFields = [
    'maxHitPoints', 'initiativeModifier', 'armorClass', 'touchArmorClass',
    'flatFootedArmorClass', 'fortitudeSave', 'reflexSave', 'willSave'
] as const;

export type StatisticField = (typeof statisticFields)[number];
export type CombatStatistics = Record<StatisticField, number>;
export type AttackMode = 'melee' | 'ranged';

export interface AttackDamage {
    diceCount: number;
    dieSides: number;
    modifier: number;
    damageType: string;
    multipliesOnCritical: boolean;
}

export interface AttackDefinition {
    name: string;
    mode: AttackMode;
    isNatural: boolean;
    attackBonus: number;
    targetsTouchArmorClass: boolean;
    criticalThreatMinimum: number;
    criticalMultiplier: number;
    damage: AttackDamage[];
    notes: string | null;
}

export interface AttackSequence {
    name: string;
    attacks: AttackDefinition[];
}

export interface TemplateWrite {
    name: string;
    statistics: CombatStatistics;
    attackSequences?: AttackSequence[];
}

export interface LibraryTemplate extends TemplateWrite {
    id: string;
    isArchived: boolean;
}

export interface VersionedTemplate {
    template: LibraryTemplate;
    etag: string;
}

export interface EditorSelection {
    kind: TemplateKind;
    saved?: VersionedTemplate;
}

export interface LibrarySession {
    userId: string;
    username: string;
}

export interface ImportResult {
    schemaVersion: number;
    characterIds: string[];
    monsterIds: string[];
}

export interface FieldIssue {
    path: string;
    code: string;
}

export const maximumImportBytes = 1024 * 1024;
