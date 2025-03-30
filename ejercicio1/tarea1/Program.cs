using System.Collections.Concurrent;

class Program
{
  static ConcurrentQueue<int> colaPacientes = new ConcurrentQueue<int>();
  static Thread[] medicos = new Thread[4];
  static bool pacientesCreated = false;

  static void Main()
  {
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

  static void CrearPaciente(int i)
  {
    colaPacientes.Enqueue(i);
    Console.WriteLine($"Paciente {i} ha llegado y espera en la cola.");
    Thread.Sleep(2000);
  }

  static void AtenderPacientes(int medicoId)
  {
    while (!pacientesCreated)
    {
      if (colaPacientes.TryDequeue(out int paciente))
      {
        Console.WriteLine($"Médico {medicoId} está atendiendo al paciente {paciente}.");
        Thread.Sleep(10000);
        Console.WriteLine($"Paciente {paciente} ha salido de la consulta del médico {medicoId}.");
      }
    }
  }
}
