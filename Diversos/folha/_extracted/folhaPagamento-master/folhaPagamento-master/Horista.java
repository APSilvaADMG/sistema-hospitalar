package folhaPagamento;

public class Horista extends Funcionario {
	double salFixo;
	int he;
	
	public Horista (String nome, String setor, String dataAdm, double salFixo, int he) {
		super(nome, setor, dataAdm);
		this.setSalFixo(salFixo);
		this.setHe(he);
	}
	
	public double getCalculoHorista(double salFixo, int he) {
		double vlrHora;
		double totHoras;
		double resultado;
				
		vlrHora = (salFixo / 40) * 1.5;
		totHoras = he * vlrHora;
		resultado = salFixo + totHoras;
		
		return resultado;
	}

	public double getSalFixo() {
		return salFixo;
	}

	public void setSalFixo(double salFixo) {
		this.salFixo = salFixo;
	}

	public int getHe() {
		return he;
	}

	public void setHe(int he) {
		this.he = he;
	}	
	
}
