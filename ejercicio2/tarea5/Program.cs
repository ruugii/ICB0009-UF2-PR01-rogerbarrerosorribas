using System;
using System.Collections.Concurrent;
using System.Threading;

public class Paciente
{
  public int Id { get; set; }
  public int LlegadaHospital { get; set; }
  public int TiempoConsulta { get; set; }
  public int WaitTime { get; set; }
  public int Estado { get; set; }
  public int prioridad { get; set; }
  public bool requiereDiagnostico { get; set; }

  public Paciente(int id, int llegadaHospital, int tiempoConsulta, bool requiereDiagnostico, int prioridad)
  {
    this.Id = id;
    this.LlegadaHospital = llegadaHospital;
    this.TiempoConsulta = tiempoConsulta;
    this.Estado = 1;
    this.WaitTime = 0;
    this.requiereDiagnostico = requiereDiagnostico;
    this.prioridad = prioridad;
  }
}

class Program
{
  static PriorityQueue<Paciente, int> colaPacientes = new PriorityQueue<Paciente, int>();
  static Random rng = new Random();
  static volatile bool pacientesCreated = false;
  static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);
  static object queueLock = new object();
  static int Emergencias = 0, EmergenciasTime = 0;
  static int Urgencias = 0, UrgenciasTime = 0;
  static int Generales = 0, GeneralesTime = 0;
  static int numberMachine = 0, total = 0;

  static void Main()
  {
    Thread[] medicos = new Thread[4];
    for (int i = 0; i < 4; i++)
    {
      int medicoId = i + 1;
      medicos[i] = new Thread(() => AtenderPacientes(medicoId));
      medicos[i].Start();
    }

    for (int i = 1; i <= 100; i++)
    {
      total++;
      CrearPaciente(i);
    }
    pacientesCreated = true;

    foreach (var medico in medicos) medico.Join();

    Console.WriteLine("--- FIN DEL DÍA --- ");
    Console.WriteLine($"Pacientes atendidos:\n\t- Emergencias: {Emergencias}\n\t- Urgencias: {Urgencias}\n\t- Consultas generales: {Generales}");
    Console.WriteLine($"Tiempo promedio de espera:\n\t- Emergencias: {EmergenciasTime / Emergencias}\n\t- Urgencias: {UrgenciasTime / Urgencias}\n\t- Consultas generales: {GeneralesTime / Generales}");
    Console.WriteLine($"Uso promedio de máquinas de diagnóstico: {numberMachine * 100 / total}%");
  }

  static void CrearPaciente(int i)
  {
    int randomId = rng.Next(1, 101);
    int randomTime = rng.Next(5, 16);
    int prioridad = rng.Next(1, 4);
    bool requiereDiagnostico = rng.Next(1, 3) == 1;

    Paciente paciente = new Paciente(randomId, Environment.TickCount, randomTime, requiereDiagnostico, prioridad);
    lock (queueLock) colaPacientes.Enqueue(paciente, paciente.prioridad);
    Thread.Sleep(2000);
  }

  static void AtenderPacientes(int medicoId)
  {
    while (true)
    {
      Paciente paciente = null;
      lock (queueLock)
      {
        if (colaPacientes.Count == 0 && pacientesCreated) break;
        if (colaPacientes.Count > 0) colaPacientes.TryDequeue(out paciente, out _);
      }

      if (paciente != null)
      {
        paciente.WaitTime = (Environment.TickCount - paciente.LlegadaHospital) / 1000;
        paciente.Estado = 2;
        Thread.Sleep(paciente.TiempoConsulta * 1000);
        paciente.Estado = 3;

        if (paciente.requiereDiagnostico)
        {
          maquinasDiagnostico.Wait();
          numberMachine++;
          Thread.Sleep(15000);
          maquinasDiagnostico.Release();
        }

        paciente.Estado = 4;
        if (paciente.prioridad == 1)
        {
          Console.WriteLine($"Paciente {paciente.Id} atendido por el doctor {medicoId}. Prioridad: {paciente.prioridad}. Tiempo de espera: {paciente.WaitTime}s");
        }

        if (paciente.prioridad == 1)
        {
          Emergencias++;
          EmergenciasTime += paciente.WaitTime;
        }
        else if (paciente.prioridad == 2)
        {
          Urgencias++;
          UrgenciasTime += paciente.WaitTime;
        }
        else if (paciente.prioridad == 3)
        {
          Generales++;
          GeneralesTime += paciente.WaitTime;
        }
      }
    }
  }
}
