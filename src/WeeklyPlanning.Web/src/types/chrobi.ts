export interface ChobiUserDto {
  id: number;
  description: string;
}

export interface ChobiProjectDto {
  id: number;
  name: string;
  clientName: string | null;
  isBillable: boolean;
}

export interface ChobiSyncResultDto {
  personsLinked: number;
  personsCreated: number;
  projectsLinked: number;
  projectsCreated: number;
}
