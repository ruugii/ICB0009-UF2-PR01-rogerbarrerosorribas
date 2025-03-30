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
  public int Prioridad { get; set; }
  public bool requiereDiagnostico { get; set; }

  public Paciente(int id, int llegadaHospital, int tiempoConsulta, bool requiereDiagnostico)
  {
    this.Id = id;
    this.LlegadaHospital = llegadaHospital;
    this.TiempoConsulta = tiempoConsulta;
    this.Estado = 1;
    /*
      Estado 1: Espera: ha llegado al hospital per aún no ha entrado en consulta
      Estado 2: Consulta: ha entrado en consulta
      Estado 3: EsperaDiagnostico: ha acabado la consulta, requiere diagnóstico, pero aún no ha sido expuesto a la máquina de diagnostico
      Estado 4: Finalizado: ha finalizado la consulta
    */

    this.WaitTime = 0;
    this.requiereDiagnostico = requiereDiagnostico;
  }
}

class Program
{
  static PriorityQueue<Paciente, int> colaPacientes = new PriorityQueue<Paciente, int>();
  static Random rng = new Random();
  static volatile bool pacientesCreated = false;
  static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);
  static object queueLock = new object();

  static void Main()
  {
    Thread[] medicos = new Thread[4];

    for (int i = 0; i < 4; i++)
    {
      int medicoId = i + 1;
      medicos[i] = new Thread(() => AtenderPacientes(medicoId));
      medicos[i].Start();
    }

    for (int i = 1; i <= 20; i++)
    {
      CrearPaciente(i);
    }

    pacientesCreated = true;

    foreach (var medico in medicos)
    {
      medico.Join();
    }
  }

    static string getEstadoString(int estado)
  {
    if (estado == 1)
    {
      return "Espera";
    }
    else if (estado == 2)
    {
      return "Consulta";
    }
    else if (estado == 3)
    {
      return "Espera de diagnostico";
    }
    else if (estado == 4)
    {
      return "Finalizado";
    }
    return "Desconocido";
  }

  static void CrearPaciente(int i)
  {
    int randomId = rng.Next(1, 101);
    int randomTime = rng.Next(5, 16);
    int prioridad = rng.Next(1, 4);

    Paciente paciente = new Paciente(randomId, i, randomTime, true, prioridad);

    lock (queueLock)
    {
      colaPacientes.Enqueue(paciente, paciente.prioridad);
    }
    Thread.Sleep(2000);

  }

  static void AtenderPacientes(int medicoId)
  {
    while (true)
    {
      Paciente paciente = null;
      int priority = 0;

      lock (queueLock)
      {
        if (colaPacientes.Count == 0 && pacientesCreated)
        {
          break;
        }

        if (colaPacientes.Count > 0)
        {
          colaPacientes.TryDequeue(out paciente, out priority);
        }
      }

      if (paciente != null)
      {
        paciente.Estado = 2;
        Thread.Sleep(paciente.TiempoConsulta * 1000);
        paciente.Estado = 3;

        maquinasDiagnostico.Wait();
        if (paciente.requiereDiagnostico)
        {
          paciente.Estado = 3;
          EnDiagnostico(paciente);
        }
        maquinasDiagnostico.Release();

        paciente.Estado = 2;
        Thread.Sleep(paciente.TiempoConsulta * 1000);
        paciente.Estado = 4;
        Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.LlegadaHospital}. Estado:{getEstadoString(paciente.Estado)}. Duración:{paciente.TiempoConsulta} segundos. Atendido por el doctor {medicoId}. Prioridad: {paciente.prioridad}");
      }
    }
  }

  static void EnDiagnostico(Paciente paciente)
  {
    Thread.Sleep(15000);
  }
}