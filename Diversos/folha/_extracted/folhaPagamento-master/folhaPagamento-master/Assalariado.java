package folhaPagamento;

public class Assalariado extends Funcionario {
	double salFixo;
	
	public Assalariado (String nome, String setor, String dataAdm, int salFixo) {
		super(nome, setor, dataAdm);
		this.setSalFixo(salFixo);		
	}
		
	public double getSalFixo() {
		return salFixo;
	}

	public void setSalFixo(int salFixo) {
		this.salFixo = salFixo;
	}
	
	
}
