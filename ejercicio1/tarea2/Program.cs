using System;
using System.Collections.Concurrent;
using System.Threading;

public class Paciente
{
    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public int Estado { get; set; }
    public int Prioridad { get; set; }

    public Paciente(int id, int llegadaHospital, int tiempoConsulta)
    {
        this.Id = id;
        this.LlegadaHospital = llegadaHospital;
        this.TiempoConsulta = tiempoConsulta;
        this.Estado = 1;
        /*
          Estado 1: Espera: ha llegado al hospital per aún no ha entrado en consulta
          Estado 2: Consulta: ha entrado en consulta
          Estado 3: Finalizado: ha finalizado la consulta
        */
    }
}

class Program
{
    static ConcurrentQueue<Paciente> colaPacientes = new ConcurrentQueue<Paciente>();
    static Random rng = new Random();
    static bool pacientesCreated = false;

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

    static void CrearPaciente(int i)
    {
        int randomId = rng.Next(1, 101); // Genera número entre 1 y 100
        int randomTime = rng.Next(1, 16); // Genera tiempo entre 1 y 15

        Paciente paciente = new Paciente(randomId, i, randomTime);

        colaPacientes.Enqueue(paciente);
        Console.WriteLine($"Paciente {randomId} ha llegado y espera en la cola.");
        Thread.Sleep(2000); // Simula la llegada de pacientes cada 2 segundos
    }

    static void AtenderPacientes(int medicoId)
    {
        while (!pacientesCreated)
        {
            if (colaPacientes.TryDequeue(out Paciente paciente))
            {
                Console.WriteLine($"Médico {medicoId} está atendiendo al paciente {paciente.Id}.");
                Thread.Sleep(10000);
                Console.WriteLine($"Paciente {paciente.Id} ha salido de la consulta del médico {medicoId}.");
            }
        }
    }
}
