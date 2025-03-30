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
  static ConcurrentQueue<Paciente> colaPacientes = new ConcurrentQueue<Paciente>();
  static Random rng = new Random();
  static bool pacientesCreated = false;
  static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);

  static void Main()
  {
    Thread[] medicos = new Thread[4];

    for (int i = 0; i < 4; i++)
    {
      int medicoId = i + 1;
      medicos[i] = new Thread(() => AtenderPacientes(medicoId));
      medicos[i].Start();
    }

    for (int i = 1; i <= 4; i++)
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

    Paciente paciente = new Paciente(randomId, i, randomTime, true);

    colaPacientes.Enqueue(paciente);
    Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.LlegadaHospital}. Estado:{getEstadoString(paciente.Estado)}. Duración:0 segundos.");
    Thread.Sleep(2000);

  }

  static void AtenderPacientes(int medicoId)
  {
    while (!pacientesCreated)
    {
      if (colaPacientes.TryDequeue(out Paciente paciente))
      {
        paciente.Estado = 2;
        Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.LlegadaHospital}. Estado:{getEstadoString(paciente.Estado)}. Duración:{paciente.WaitTime} segundos. Atendido por el doctor {medicoId}");
        Thread.Sleep(paciente.TiempoConsulta * 1000);
        paciente.Estado = 3;
        maquinasDiagnostico.Wait();
        if (paciente.requiereDiagnostico)
        {

          paciente.Estado = 3;
          Console.WriteLine($"Paciente {paciente.Id} en espera de diagnóstico.");

          EnDiagnostico(paciente);
        }
        maquinasDiagnostico.Release();
        paciente.Estado = 2;
        Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.LlegadaHospital}. Estado:{getEstadoString(paciente.Estado)}. Duración:{paciente.WaitTime} segundos. Atendido por el doctor {medicoId}");
        Thread.Sleep(paciente.TiempoConsulta * 1000);
        paciente.Estado = 4;
        Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.LlegadaHospital}. Estado:{getEstadoString(paciente.Estado)}. Duración:{paciente.TiempoConsulta} segundos. Atendido por el doctor {medicoId}");
      }
    }

    static void EnDiagnostico(Paciente paciente)
    {
      Console.WriteLine($"Paciente {paciente.Id} está en diagnóstico durante 15 segundos.");
      Thread.Sleep(15000);
      Console.WriteLine($"Paciente {paciente.Id} ha completado el diagnóstico.");
    }
  }
}
